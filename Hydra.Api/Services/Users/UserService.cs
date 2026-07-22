using Hydra.Api.Contracts.Common;
using Hydra.Api.Contracts.Customers;
using Hydra.Api.Contracts.Users;
using Hydra.Api.Contracts.Venues;
using Hydra.Api.Data;
using Hydra.Api.Mapping;
using Hydra.Api.Models;
using Microsoft.EntityFrameworkCore;
using Hydra.Api.Repositories.Users;
using Hydra.Api.Services.Customers;
using Hydra.Api.Services.Venues;

namespace Hydra.Api.Services.Users;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepo;
    private readonly ICustomerService _customerService;
    private readonly IVenueService _venueService;
    private readonly AppDbContext _context;

    public UserService(
        IUserRepository userRepo,
        ICustomerService customerService,
        IVenueService venueService,
        AppDbContext context)
    {
        _userRepo = userRepo;
        _customerService = customerService;
        _venueService = venueService;
        _context = context;
    }

    public async Task<PagedResult<UserDto>> GetAllUsersAsync(int page, int pageSize, string? search = null, string? role = null, CancellationToken ct = default)
    {
        var safeSize = Math.Clamp(pageSize, 1, 100);
        var skip = (Math.Max(1, page) - 1) * safeSize;

        UserRole? roleFilter = null;
        if (!string.IsNullOrWhiteSpace(role) && Enum.TryParse<UserRole>(role, ignoreCase: true, out var parsed))
            roleFilter = parsed;

        var (items, total) = await _userRepo.GetAllAsync(skip, safeSize, search, roleFilter, ct);
        return new PagedResult<UserDto>(items.Select(u => u.ToDto()).ToList(), total, page, safeSize);
    }

    public async Task<bool> UpdateUserEmailAsync(Guid id, string email, CancellationToken ct = default)
    {
        var normalized = (email ?? "").Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalized))
            throw new InvalidOperationException("Email is required");

        var existing = await _userRepo.GetByEmailAsync(normalized, ct);
        if (existing is not null && existing.Id != id)
            throw new InvalidOperationException($"User with email '{normalized}' already exists");

        var user = await _userRepo.GetByIdAsync(id, ct);
        if (user is null)
            return false;

        user.Email = normalized;
        await _userRepo.UpdateAsync(user, ct);
        return true;
    }

    public async Task<UserDto?> GetUserByIdAsync(Guid id, CancellationToken ct = default)
    {
        var user = await _userRepo.GetByIdAsync(id, ct);
        return user?.ToDto();
    }

    public async Task<UserDto?> GetUserByEmailAsync(string email, CancellationToken ct = default)
    {
        var user = await _userRepo.GetByEmailAsync(email, ct);
        return user?.ToDto();
    }

    private async Task<UserDto> CreateUserAsync(CreateUserRequest request, CancellationToken ct = default)
    {
        var user = request.ToModel();
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

        var created = await _userRepo.AddAsync(user, ct);

        return created.ToDto();
    }

    public async Task<bool> UpdateUserPasswordAsync(Guid id, UpdateUserRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.CurrentPassword))
            throw new InvalidOperationException("Current password is required");

        if (string.IsNullOrWhiteSpace(request.NewPassword))
            throw new InvalidOperationException("New password is required");

        ValidatePassword(request.NewPassword);

        var user = await _userRepo.GetByIdAsync(id, ct);
        if (user is null)
            return false;

        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
            throw new InvalidOperationException("Current password is incorrect");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);

        await _userRepo.UpdateAsync(user, ct);
        return true;
    }

    public async Task<bool> DeleteUserAsync(Guid id, CancellationToken ct = default)
    {
        var user = await _userRepo.GetByIdAsync(id, ct);
        if (user is null)
            return false;

        using var tx = await _context.Database.BeginTransactionAsync(ct);

        // The user's Venue/Customer (and the venue's photos/pricing/events/rules)
        // cascade when the user row is deleted. But VenueRating has *restrict* FKs
        // to Booking and Customer, so ratings + bookings must be removed first or
        // the cascade fails. Clear them explicitly for the affected venue/customer.
        if (user.Role == UserRole.Admin)
        {
            var venueId = await _context.Venues
                .Where(v => v.UserId == id)
                .Select(v => (Guid?)v.Id)
                .FirstOrDefaultAsync(ct);

            if (venueId is not null)
            {
                await _context.VenueRatings.Where(r => r.VenueId == venueId).ExecuteDeleteAsync(ct);
                await _context.Bookings.Where(b => b.VenueId == venueId).ExecuteDeleteAsync(ct);
            }
        }
        else if (user.Role == UserRole.Customer)
        {
            var customerId = await _context.Customers
                .Where(c => c.UserId == id)
                .Select(c => (Guid?)c.Id)
                .FirstOrDefaultAsync(ct);

            if (customerId is not null)
            {
                await _context.VenueRatings.Where(r => r.CustomerId == customerId).ExecuteDeleteAsync(ct);
                await _context.Bookings.Where(b => b.CustomerId == customerId).ExecuteDeleteAsync(ct);
            }
        }

        await _userRepo.DeleteAsync(id, ct);
        await tx.CommitAsync(ct);
        return true;
    }

    public async Task<(UserDto User, CustomerDto Customer)> RegisterCustomerWithProfileAsync(
    RegisterCustomerRequest request,
    CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            throw new InvalidOperationException("Email is required");

        if (string.IsNullOrWhiteSpace(request.Password))
            throw new InvalidOperationException("Password is required");

        ValidatePassword(request.Password);

        if (string.IsNullOrWhiteSpace(request.Name))
            throw new InvalidOperationException("Name is required");

        if (string.IsNullOrWhiteSpace(request.Phone))
            throw new InvalidOperationException("Phone is required");

        var existingUser = await _userRepo.GetByEmailAsync(request.Email, ct);
        if (existingUser is not null)
        {
            throw new InvalidOperationException($"User with email '{request.Email}' already exists");
        }

        using var transaction = await _context.Database.BeginTransactionAsync(ct);

        try
        {
            var userRequest = new CreateUserRequest(
                Email: request.Email,
                Password: request.Password,
                Role: "Customer"
            );

            var user = await CreateUserAsync(userRequest, ct);

            var customerRequest = new CreateCustomerRequest(
                UserId: user.Id,
                Email: request.Email,
                Name: request.Name,
                Phone: request.Phone
            );

            var customer = await _customerService.CreateCustomerAsync(customerRequest, ct);

            await transaction.CommitAsync(ct);

            return (user, customer);
        }
        catch (Exception)
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }


    public async Task<(UserDto User, VenueDto Venue)> RegisterVenueWithProfileAsync(
    RegisterVenueRequest request,
    CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            throw new InvalidOperationException("Email is required");

        if (string.IsNullOrWhiteSpace(request.Name))
            throw new InvalidOperationException("Name is required");

        if (request.Capacity == 0)
            throw new InvalidOperationException("Capacity is required");

        if (string.IsNullOrWhiteSpace(request.Address))
            throw new InvalidOperationException("Address is required");

        var isGoogleAuth = string.IsNullOrWhiteSpace(request.Password);

        if (!isGoogleAuth)
            ValidatePassword(request.Password!);

        var existingUser = await _userRepo.GetByEmailAsync(request.Email, ct);
        if (existingUser is not null)
            throw new InvalidOperationException($"User with email '{request.Email}' already exists");

        using var transaction = await _context.Database.BeginTransactionAsync(ct);

        try
        {
            var passwordHash = isGoogleAuth
                ? BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString())
                : BCrypt.Net.BCrypt.HashPassword(request.Password);

            var user = await _userRepo.AddAsync(new User
            {
                Email = request.Email.Trim().ToLowerInvariant(),
                PasswordHash = passwordHash,
                Role = UserRole.Admin,
                IsEmailVerified = true,
                AuthProvider = isGoogleAuth ? AuthProvider.Google : AuthProvider.Email
            }, ct);

            var venueRequest = new CreateVenueRequest(
                UserId: user.Id,
                Name: request.Name,
                Address: request.Address,
                Capacity: request.Capacity,
                VenueTypeId: request.VenueTypeId,
                Description: request.Description,
                Location: request.Location
            );

            var venue = await _venueService.CreateVenueAsync(venueRequest, ct);

            await transaction.CommitAsync(ct);

            return (user.ToDto(), venue);
        }
        catch (Exception)
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    private static void ValidatePassword(string password)
    {
        if (password.Length < 10)
            throw new InvalidOperationException("Password must be at least 10 characters long.");

        if (!password.Any(char.IsUpper))
            throw new InvalidOperationException("Password must contain at least one uppercase letter.");

        if (!password.Any(char.IsDigit))
            throw new InvalidOperationException("Password must contain at least one digit.");

        if (!password.Any(c => !char.IsLetterOrDigit(c)))
            throw new InvalidOperationException("Password must contain at least one special character.");
    }
}