using Hydra.Api.Data;
using Hydra.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Hydra.Api.Repositories.Customers;

public class CustomerRepository : ICustomerRepository
{
    private readonly AppDbContext _context;

    public CustomerRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Customer>> GetAllAsync(CancellationToken ct = default)
    {
        return await _context.Customers
            .OrderByDescending(c => c.CreatedAtUtc)
            .ToListAsync(ct);
    }

    public async Task<Customer?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Customers
            .FirstOrDefaultAsync(c => c.Id == id, ct);
    }

    public async Task<Customer?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        return await _context.Customers
            .FirstOrDefaultAsync(c => c.Email == email, ct);
    }

    public async Task<Customer?> GetByPhoneAsync(string phone, CancellationToken ct = default)
    {
        return await _context.Customers
            .FirstOrDefaultAsync(c => c.Phone == phone, ct);
    }

    public async Task<Customer> AddAsync(Customer customer, CancellationToken ct = default)
    {
        _context.Customers.Add(customer);
        await _context.SaveChangesAsync(ct);
        return customer;
    }

    public async Task UpdateAsync(Customer customer, CancellationToken ct = default)
    {
        _context.Customers.Update(customer);
        await _context.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var customer = await _context.Customers.FindAsync(new object[] { id }, ct);
        if (customer is not null)
        {
            _context.Customers.Remove(customer);
            await _context.SaveChangesAsync(ct);
        }
    }
}