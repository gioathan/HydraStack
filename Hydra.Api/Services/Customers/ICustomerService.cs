using Hydra.Api.Contracts.Customers;

namespace Hydra.Api.Services.Customers;

public interface ICustomerService
{
    Task<List<CustomerDto>> GetAllCustomersAsync(CancellationToken ct = default);
    Task<CustomerDto?> GetCustomerByIdAsync(Guid id, CancellationToken ct = default);
    Task<CustomerDto?> GetCustomerByEmailAsync(string email, CancellationToken ct = default);
    Task<CustomerDto?> GetCustomerByPhoneAsync(string phone, CancellationToken ct = default);
    Task<CustomerDto> CreateCustomerAsync(CreateCustomerRequest request, CancellationToken ct = default);
    Task<CustomerDto?> UpdateCustomerAsync(Guid id, CreateCustomerRequest request, CancellationToken ct = default);
    Task<bool> DeleteCustomerAsync(Guid id, CancellationToken ct = default);
}