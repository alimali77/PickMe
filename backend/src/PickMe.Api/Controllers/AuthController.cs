using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PickMe.Api.Common;
using PickMe.Application.Abstractions;
using PickMe.Application.Auth;

namespace PickMe.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IAuthService auth, ICurrentUser currentUser) : ControllerBase
{
    private readonly IAuthService _auth = auth;
    private readonly ICurrentUser _currentUser = currentUser;

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterCustomerDto dto, CancellationToken ct)
        => (await _auth.RegisterCustomerAsync(dto, ct)).ToActionResult(StatusCodes.Status201Created);

    [HttpPost("verify-email")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailDto dto, CancellationToken ct)
        => (await _auth.VerifyEmailAsync(dto, ct)).ToActionResult();

    [HttpPost("resend-verification")]
    [AllowAnonymous]
    public async Task<IActionResult> ResendVerification([FromBody] ResendVerificationDto dto, CancellationToken ct)
        => (await _auth.ResendVerificationAsync(dto, ct)).ToActionResult();

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginDto dto, CancellationToken ct)
        => (await _auth.LoginAsync(dto, ct)).ToActionResult();

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh([FromBody] RefreshDto dto, CancellationToken ct)
        => (await _auth.RefreshAsync(dto, ct)).ToActionResult();

    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<IActionResult> Logout([FromBody] LogoutDto dto, CancellationToken ct)
        => (await _auth.LogoutAsync(dto, ct)).ToActionResult();

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto, CancellationToken ct)
        => (await _auth.ForgotPasswordAsync(dto, ct)).ToActionResult();

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto, CancellationToken ct)
        => (await _auth.ResetPasswordAsync(dto, ct)).ToActionResult();

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        var userId = _currentUser.UserId ?? Guid.Empty;
        return (await _auth.GetCurrentUserAsync(userId, ct)).ToActionResult();
    }

    [HttpPatch("me")]
    [Authorize]
    public async Task<IActionResult> UpdateMe([FromBody] UpdateProfileDto dto, CancellationToken ct)
    {
        var userId = _currentUser.UserId ?? Guid.Empty;
        return (await _auth.UpdateProfileAsync(userId, dto, ct)).ToActionResult();
    }

    [HttpPatch("me/password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto, CancellationToken ct)
    {
        var userId = _currentUser.UserId ?? Guid.Empty;
        return (await _auth.ChangePasswordAsync(userId, dto, ct)).ToActionResult();
    }
}
