using Hydra.Api.Contracts.Common;
using Hydra.Api.Contracts.Customers;

namespace Hydra.Api.Services.Customers;

public interface ICustomerService
{
    Task<PagedResult<CustomerDto>> GetAllCustomersAsync(int page, int pageSize, CancellationToken ct = default);
    Task<CustomerDto?> GetCustomerByIdAsync(Guid id, CancellationToken ct = default);
    Task<CustomerDto?> GetCustomerByEmailAsync(string email, CancellationToken ct = default);
    Task<CustomerDto?> GetCustomerByPhoneAsync(string phone, CancellationToken ct = default);
    Task<CustomerDto> CreateCustomerAsync(CreateCustomerRequest request, CancellationToken ct = default);
    Task<CustomerDto?> UpdateCustomerAsync(Guid id, UpdateCustomerRequest request, CancellationToken ct = default);
}