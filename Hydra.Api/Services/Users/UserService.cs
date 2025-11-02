using Hydra.Api.Contracts.Users;
using Hydra.Api.Mapping;
using Hydra.Api.Repositories.Users;
using BCrypt.Net;

namespace Hydra.Api.Services.Users;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepo;

    public UserService(IUserRepository userRepo)
    {
        _userRepo = userRepo;
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

    public async Task<UserDto> CreateUserAsync(CreateUserRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            throw new InvalidOperationException("Email is required");
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            throw new InvalidOperationException("Password is required");
        }

        if (request.Password.Length < 8)
        {
            throw new InvalidOperationException("Password must be at least 8 characters long");
        }

        var existingUser = await _userRepo.GetByEmailAsync(request.Email, ct);
        if (existingUser is not null)
        {
            throw new InvalidOperationException($"User with email '{request.Email}' already exists");
        }

        var user = request.ToModel();
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

        var created = await _userRepo.AddAsync(user, ct);

        return created.ToDto();
    }

    public async Task<bool> DeleteUserAsync(Guid id, CancellationToken ct = default)
    {
        var user = await _userRepo.GetByIdAsync(id, ct);
        if (user is null)
            return false;

        await _userRepo.DeleteAsync(id, ct);
        return true;
    }

    public async Task<UserDto?> ValidateCredentialsAsync(string email, string password, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return null;
        }

        var user = await _userRepo.GetByEmailAsync(email, ct);
        if (user is null)
        {
            return null;
        }

        var isValid = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
        if (!isValid)
        {
            return null;
        }

        return user.ToDto();
    }
}