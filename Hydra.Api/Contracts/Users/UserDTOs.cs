using Hydra.Api.Contracts.Customers;
using Hydra.Api.Contracts.Venues;

namespace Hydra.Api.Contracts.Users;

public record UserDto(
    Guid Id,
    string Email,
    string Role,
    bool IsEmailVerified
);

public record CreateUserRequest(
    string Email,
    string Password,
    string Role
);

public record UpdateUserRequest(
    string CurrentPassword,
    string NewPassword
);

public record RegisterCustomerRequest(
    string Email,
    string Password,
    string Name,
    string Phone
);

public record RegisterVenueRequest(
    string Email,
    string Password,
    string Name,
    string Address,
    int Capacity,
    Guid VenueTypeId,
    string? Description = null);

public record LoginRequest(string Email, string Password);

public record LoginResponse(
    UserDto User,
    string Token,
    Guid? CustomerId = null,
    Guid? VenueId = null);

public record CustomerAuthResponse(
    UserDto User,
    CustomerDto Customer,
    string Token
);

public record VenueAuthResponse(
    UserDto User,
    VenueDto Venue,
    string Token
);
