using Hydra.Api.Contracts.Customers;
using Hydra.Api.Contracts.Venues;

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

public record UpdateUserRequest(
    string Password
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
    Guid VenueTypeId
);

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
