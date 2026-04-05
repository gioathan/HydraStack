using Hydra.Api.Contracts.Customers;
using Hydra.Api.Contracts.Users;
using Hydra.Api.Contracts.Venues;

namespace Hydra.Api.Services.Users;

public interface IUserService
{
    Task<List<UserDto>> GetAllUsersAsync(CancellationToken ct = default);
    Task<UserDto?> GetUserByIdAsync(Guid id, CancellationToken ct = default);
    Task<UserDto?> GetUserByEmailAsync(string email, CancellationToken ct = default);
    Task<bool> DeleteUserAsync(Guid id, CancellationToken ct = default);
    Task<bool> UpdateUserPasswordAsync(Guid id, UpdateUserRequest request, CancellationToken ct = default);

    Task<(UserDto User, CustomerDto Customer)> RegisterCustomerWithProfileAsync(
    RegisterCustomerRequest request,
    CancellationToken ct = default);

    Task<(UserDto User, VenueDto Venue)> RegisterVenueWithProfileAsync(
    RegisterVenueRequest request,
    CancellationToken ct = default);
}