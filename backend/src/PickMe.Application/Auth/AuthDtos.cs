namespace PickMe.Application.Auth;

public sealed record RegisterCustomerDto(
    string FirstName,
    string LastName,
    string Email,
    string Phone,
    string Password,
    string PasswordConfirm,
    bool KvkkAccepted);

public sealed record LoginDto(string Email, string Password);
public sealed record ForgotPasswordDto(string Email);
public sealed record ResetPasswordDto(string Token, string Password, string PasswordConfirm);
public sealed record ChangePasswordDto(string CurrentPassword, string NewPassword, string NewPasswordConfirm);
public sealed record UpdateProfileDto(string FirstName, string LastName, string Phone);
public sealed record RefreshDto(string RefreshToken);
public sealed record LogoutDto(string RefreshToken);
public sealed record VerifyEmailDto(string Token);
public sealed record ResendVerificationDto(string Email);

public sealed record AuthTokensDto(
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiresAt,
    DateTime RefreshTokenExpiresAt);

public sealed record CurrentUserDto(
    Guid Id,
    string Email,
    string Role,
    string FirstName,
    string LastName,
    string Phone,
    bool MustChangePassword,
    bool EmailConfirmed);
