namespace Hydra.Api.Contracts.Users;

public record UserDto(
    Guid Id,
    string Email,
    string Role
);

public record CreateUserRequest(
    string Email,
    string Password,
    string Role
);

public record AuthResponse(UserDto User, string Token);
