namespace Hydra.Api.Services.Notifications;

// Placeholder — implement and register when email sending is added.
public interface IEmailService
{
    Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default);
}
