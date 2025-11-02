using Hydra.Api.Contracts.Customers;
using Hydra.Api.Mapping;
using Hydra.Api.Repositories.Customers;

namespace Hydra.Api.Services.Customers;

public class CustomerService : ICustomerService
{
    private readonly ICustomerRepository _customerRepo;

    public CustomerService(ICustomerRepository customerRepo)
    {
        _customerRepo = customerRepo;
    }

    public async Task<List<CustomerDto>> GetAllCustomersAsync(CancellationToken ct = default)
    {
        var customers = await _customerRepo.GetAllAsync(ct);
        return customers.Select(c => c.ToDto()).ToList();
    }

    public async Task<CustomerDto?> GetCustomerByIdAsync(Guid id, CancellationToken ct = default)
    {
        var customer = await _customerRepo.GetByIdAsync(id, ct);
        return customer?.ToDto();
    }

    public async Task<CustomerDto?> GetCustomerByEmailAsync(string email, CancellationToken ct = default)
    {
        var customer = await _customerRepo.GetByEmailAsync(email, ct);
        return customer?.ToDto();
    }

    public async Task<CustomerDto?> GetCustomerByPhoneAsync(string phone, CancellationToken ct = default)
    {
        var customer = await _customerRepo.GetByPhoneAsync(phone, ct);
        return customer?.ToDto();
    }

    public async Task<CustomerDto> CreateCustomerAsync(CreateCustomerRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email) && string.IsNullOrWhiteSpace(request.Phone))
        {
            throw new InvalidOperationException("Either Email or Phone must be provided");
        }

        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            var existingByEmail = await _customerRepo.GetByEmailAsync(request.Email, ct);
            if (existingByEmail is not null)
            {
                throw new InvalidOperationException($"Customer with email '{request.Email}' already exists");
            }
        }

        if (!string.IsNullOrWhiteSpace(request.Phone))
        {
            var existingByPhone = await _customerRepo.GetByPhoneAsync(request.Phone, ct);
            if (existingByPhone is not null)
            {
                throw new InvalidOperationException($"Customer with phone '{request.Phone}' already exists");
            }
        }

        var customer = request.ToModel();
        var created = await _customerRepo.AddAsync(customer, ct);

        return created.ToDto();
    }

    public async Task<CustomerDto?> UpdateCustomerAsync(Guid id, CreateCustomerRequest request, CancellationToken ct = default)
    {
        var customer = await _customerRepo.GetByIdAsync(id, ct);
        if (customer is null)
            return null;

        if (string.IsNullOrWhiteSpace(request.Email) && string.IsNullOrWhiteSpace(request.Phone))
        {
            throw new InvalidOperationException("Either Email or Phone must be provided");
        }

        if (!string.IsNullOrWhiteSpace(request.Email) && request.Email != customer.Email)
        {
            var existingByEmail = await _customerRepo.GetByEmailAsync(request.Email, ct);
            if (existingByEmail is not null && existingByEmail.Id != id)
            {
                throw new InvalidOperationException($"Customer with email '{request.Email}' already exists");
            }
        }

        if (!string.IsNullOrWhiteSpace(request.Phone) && request.Phone != customer.Phone)
        {
            var existingByPhone = await _customerRepo.GetByPhoneAsync(request.Phone, ct);
            if (existingByPhone is not null && existingByPhone.Id != id)
            {
                throw new InvalidOperationException($"Customer with phone '{request.Phone}' already exists");
            }
        }

        customer.Name = request.Name;
        customer.Email = request.Email;
        customer.Phone = request.Phone;
        customer.Locale = request.Locale;
        customer.MarketingOptIn = request.MarketingOptIn;

        await _customerRepo.UpdateAsync(customer, ct);

        return customer.ToDto();
    }

    public async Task<bool> DeleteCustomerAsync(Guid id, CancellationToken ct = default)
    {
        var customer = await _customerRepo.GetByIdAsync(id, ct);
        if (customer is null)
            return false;

        await _customerRepo.DeleteAsync(id, ct);
        return true;
    }
}