namespace Hydra.Api.Contracts.Auth;

public record GoogleAuthRequest(string IdToken);
public record VerifyEmailRequest(Guid UserId, string Code);
public record ResendVerificationRequest(Guid UserId);
public record ForgotPasswordRequest(string Email);
public record ResetPasswordRequest(string Email, string Code, string NewPassword);
