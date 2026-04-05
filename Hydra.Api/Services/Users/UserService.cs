using Hydra.Api.Contracts.Customers;
using Hydra.Api.Contracts.Users;
using Hydra.Api.Mapping;
using Hydra.Api.Repositories.Users;
using Hydra.Api.Services.Customers;
using Hydra.Api.Data;
using Hydra.Api.Contracts.Venues;
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

    public async Task<List<UserDto>> GetAllUsersAsync(CancellationToken ct = default)
    {
        var users = await _userRepo.GetAllAsync(ct);
        return users.Select(u => u.ToDto()).ToList();
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
        if (string.IsNullOrWhiteSpace(request.Password))
            throw new InvalidOperationException("Password is required");

        if (request.Password.Length < 8)
            throw new InvalidOperationException("Password must be at least 8 characters long");

        var user = await _userRepo.GetByIdAsync(id, ct);
        if (user is null)
            return false;

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

        await _userRepo.UpdateAsync(user, ct);
        return true;
    }

    public async Task<bool> DeleteUserAsync(Guid id, CancellationToken ct = default)
    {
        var user = await _userRepo.GetByIdAsync(id, ct);
        if (user is null)
            return false;

        await _userRepo.DeleteAsync(id, ct);
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

        if (request.Password.Length < 8)
            throw new InvalidOperationException("Password must be at least 8 characters long");

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

        if (string.IsNullOrWhiteSpace(request.Password))
            throw new InvalidOperationException("Password is required");

        if (request.Password.Length < 8)
            throw new InvalidOperationException("Password must be at least 8 characters long");

        if (string.IsNullOrWhiteSpace(request.Name))
            throw new InvalidOperationException("Name is required");

        if (request.Capacity == 0)
            throw new InvalidOperationException("Capacity is required");

        if (string.IsNullOrWhiteSpace(request.Address))
            throw new InvalidOperationException("Address is required");

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
                Role: "Admin"
            );

            var user = await CreateUserAsync(userRequest, ct);

            var venueRequest = new CreateVenueRequest(
                UserId: user.Id,
                Name: request.Name,
                Address: request.Address,
                Capacity: request.Capacity,
                VenueTypeId: request.VenueTypeId
            );

            var venue = await _venueService.CreateVenueAsync(venueRequest, ct);

            await transaction.CommitAsync(ct);

            return (user, venue);
        }
        catch (Exception)
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }
}