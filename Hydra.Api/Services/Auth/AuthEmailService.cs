using Hydra.Api.Caching;
using Hydra.Api.Services.Email;

namespace Hydra.Api.Services.Auth;

public class AuthEmailService : IAuthEmailService
{
    private readonly ICache _cache;
    private readonly IEmailService _emailService;

    private static readonly TimeSpan OtpTtl = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan ResendWindowTtl = TimeSpan.FromMinutes(10);
    private const int MaxResends = 3;

    private static string EmailVerifyKey(Guid userId) => $"hb:email-verify:{userId}";
    private static string ResendWindowKey(Guid userId) => $"hb:email-verify-resend:{userId}";
    private static string PasswordResetKey(Guid userId) => $"hb:password-reset:{userId}";

    public AuthEmailService(ICache cache, IEmailService emailService)
    {
        _cache = cache;
        _emailService = emailService;
    }

    public async Task SendVerificationOtpAsync(Guid userId, string email, CancellationToken ct = default)
    {
        var otp = GenerateOtp();
        await _cache.SetAsync(EmailVerifyKey(userId), otp, OtpTtl, ct);
        await _emailService.SendAsync(email, "Verify your Hydra account", VerificationEmailHtml(otp), ct);
    }

    public async Task<bool> VerifyAndConsumeEmailOtpAsync(Guid userId, string code, CancellationToken ct = default)
    {
        var stored = await _cache.GetAsync<string>(EmailVerifyKey(userId), ct);
        if (stored is null || stored != code)
            return false;

        await _cache.RemoveAsync(EmailVerifyKey(userId), ct);
        return true;
    }

    public async Task<bool> ResendVerificationOtpAsync(Guid userId, string email, CancellationToken ct = default)
    {
        var windowKey = ResendWindowKey(userId);
        var window = await _cache.GetAsync<ResendWindow>(windowKey, ct);

        if (window is not null && DateTimeOffset.UtcNow.Ticks < window.ExpiresAtTicks)
        {
            if (window.Count >= MaxResends)
                return false;

            var remaining = TimeSpan.FromTicks(window.ExpiresAtTicks - DateTimeOffset.UtcNow.Ticks);
            await _cache.SetAsync(windowKey, window with { Count = window.Count + 1 }, remaining, ct);
        }
        else
        {
            var expiresAt = DateTimeOffset.UtcNow.Add(ResendWindowTtl);
            await _cache.SetAsync(windowKey, new ResendWindow(1, expiresAt.Ticks), ResendWindowTtl, ct);
        }

        await SendVerificationOtpAsync(userId, email, ct);
        return true;
    }

    public async Task SendPasswordResetOtpAsync(Guid userId, string email, CancellationToken ct = default)
    {
        var otp = GenerateOtp();
        await _cache.SetAsync(PasswordResetKey(userId), otp, OtpTtl, ct);
        await _emailService.SendAsync(email, "Reset your Hydra password", PasswordResetEmailHtml(otp), ct);
    }

    public async Task<bool> VerifyAndConsumePasswordResetOtpAsync(Guid userId, string code, CancellationToken ct = default)
    {
        var stored = await _cache.GetAsync<string>(PasswordResetKey(userId), ct);
        if (stored is null || stored != code)
            return false;

        await _cache.RemoveAsync(PasswordResetKey(userId), ct);
        return true;
    }

    private static string GenerateOtp() => Random.Shared.Next(100_000, 1_000_000).ToString("D6");

    private record ResendWindow(int Count, long ExpiresAtTicks);

    private static string VerificationEmailHtml(string otp) => $"""
        <div style="font-family:sans-serif;max-width:480px;margin:auto;padding:32px;border:1px solid #e5e7eb;border-radius:8px">
          <h2 style="color:#1a1a2e;margin-bottom:8px">Verify your email</h2>
          <p style="color:#374151">Use the code below to verify your Hydra account. It expires in 15 minutes.</p>
          <div style="font-size:40px;font-weight:bold;letter-spacing:10px;text-align:center;padding:28px 0;color:#1a1a2e;background:#f9fafb;border-radius:6px;margin:24px 0">{otp}</div>
          <p style="color:#9ca3af;font-size:13px">If you didn't create an account, you can safely ignore this email.</p>
        </div>
        """;

    private static string PasswordResetEmailHtml(string otp) => $"""
        <div style="font-family:sans-serif;max-width:480px;margin:auto;padding:32px;border:1px solid #e5e7eb;border-radius:8px">
          <h2 style="color:#1a1a2e;margin-bottom:8px">Reset your password</h2>
          <p style="color:#374151">Use the code below to reset your Hydra password. It expires in 15 minutes.</p>
          <div style="font-size:40px;font-weight:bold;letter-spacing:10px;text-align:center;padding:28px 0;color:#1a1a2e;background:#f9fafb;border-radius:6px;margin:24px 0">{otp}</div>
          <p style="color:#9ca3af;font-size:13px">If you didn't request a password reset, you can safely ignore this email.</p>
        </div>
        """;
}
