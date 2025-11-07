using Hydra.Api.Contracts.Users;

namespace Hydra.Api.Services.Users;

public interface IUserService
{
    Task<List<UserDto>> GetAllUsersAsync(CancellationToken ct = default);
    Task<UserDto?> GetUserByIdAsync(Guid id, CancellationToken ct = default);
    Task<UserDto?> GetUserByEmailAsync(string email, CancellationToken ct = default);
    Task<UserDto> CreateUserAsync(CreateUserRequest request, CancellationToken ct = default);
    Task<bool> DeleteUserAsync(Guid id, CancellationToken ct = default);
    Task<UserDto?> ValidateCredentialsAsync(string email, string password, CancellationToken ct = default);
}