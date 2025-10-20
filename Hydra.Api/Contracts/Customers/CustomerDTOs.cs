namespace Hydra.Api.Contracts.Customers;

public record CreateCustomerRequest(
    string? Email,
    string? Phone,
    string Locale = "en",
    bool MarketingOptIn = false,
    string? Name = null);

public record CustomerDto(
    Guid Id,
    string? Email,
    string? Phone,
    string Locale,
    bool MarketingOptIn,
    DateTime CreatedAtUtc,
    string? Name);
