using Resend;

namespace Hydra.Api.Services.Email;

public class ResendEmailService : IEmailService
{
    private readonly IResend _resend;
    private readonly ILogger<ResendEmailService> _logger;
    private readonly string _from;

    public ResendEmailService(IResend resend, ILogger<ResendEmailService> logger, IConfiguration config)
    {
        _resend = resend;
        _logger = logger;
        // Resend's shared sandbox domain (onboarding@resend.dev) only delivers to your
        // own Resend account email, so this MUST be overridden to an address on your
        // own verified domain (e.g. "Local Bee <noreply@localbee.gr>") once verified.
        _from = config["Resend:FromAddress"] ?? "Local Bee <onboarding@resend.dev>";
    }

    public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
    {
        try
        {
            var message = new EmailMessage();
            message.From = _from;
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
