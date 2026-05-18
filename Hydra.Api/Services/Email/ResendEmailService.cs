using Resend;

namespace Hydra.Api.Services.Email;

public class ResendEmailService : IEmailService
{
    private readonly IResend _resend;
    private readonly ILogger<ResendEmailService> _logger;

    private const string From = "Hydra Booking <onboarding@resend.dev>";

    public ResendEmailService(IResend resend, ILogger<ResendEmailService> logger)
    {
        _resend = resend;
        _logger = logger;
    }

    public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
    {
        try
        {
            var message = new EmailMessage();
            message.From = From;
            message.To.Add(to);
            message.Subject = subject;
            message.HtmlBody = htmlBody;
            await _resend.EmailSendAsync(message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To} with subject {Subject}", to, subject);
        }
    }
}
