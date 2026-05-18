namespace Hydra.Api.Services.Auth;

public interface IAuthEmailService
{
    Task SendVerificationOtpAsync(Guid userId, string email, CancellationToken ct = default);
    Task<bool> VerifyAndConsumeEmailOtpAsync(Guid userId, string code, CancellationToken ct = default);
    /// <summary>Returns false if the user has hit the resend rate limit (3 per 10 minutes).</summary>
    Task<bool> ResendVerificationOtpAsync(Guid userId, string email, CancellationToken ct = default);
    Task SendPasswordResetOtpAsync(Guid userId, string email, CancellationToken ct = default);
    Task<bool> VerifyAndConsumePasswordResetOtpAsync(Guid userId, string code, CancellationToken ct = default);
}
