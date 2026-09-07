using Lca.Api.Contracts;
using Lca.Api.Security;
using Lca.Core.Security;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Lca.Api.Controllers;

[ApiController]
public sealed class AuthenticationController(
    ApplicationAuthenticationService authenticationService,
    IAccountRecoveryService accountRecoveryService,
    ICurrentUser currentUser,
    ITenantContext tenantContext) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("api/v1/auth/login")]
    public async Task<ActionResult<AccessTokenResponse>> TenantLogin(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        IssuedAccessToken? token = await authenticationService.LoginTenantAsync(
            request.UserNameOrEmail,
            request.Password,
            cancellationToken);
        return token is null
            ? Unauthorized()
            : Ok(new AccessTokenResponse(token.Value, "Bearer", token.ExpiresAtUtc));
    }

    [AllowAnonymous]
    [HttpPost("api/v1/platform/auth/login")]
    public async Task<ActionResult<AccessTokenResponse>> PlatformLogin(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        IssuedAccessToken? token = await authenticationService.LoginPlatformAsync(
            request.UserNameOrEmail,
            request.Password,
            cancellationToken);
        return token is null
            ? Unauthorized()
            : Ok(new AccessTokenResponse(token.Value, "Bearer", token.ExpiresAtUtc));
    }

    [HttpGet("api/v1/auth/me")]
    [Authorize(Policy = Policies.TenantAccess)]
    public ActionResult<CurrentAccountResponse> TenantMe() => Ok(new CurrentAccountResponse(
        currentUser.UserId!,
        "tenant",
        tenantContext.TenantId!.Value.Value));

    [HttpGet("api/v1/platform/auth/me")]
    [Authorize(Policy = Policies.PlatformAdmin)]
    public ActionResult<CurrentAccountResponse> PlatformMe() => Ok(new CurrentAccountResponse(
        currentUser.UserId!,
        "platform",
        null));

    [AllowAnonymous]
    [EnableRateLimiting("account-recovery-forgot")]
    [HttpPost("api/v1/auth/forgot-password")]
    public async Task<ActionResult<ForgotPasswordResponse>> TenantForgotPassword(
        ForgotPasswordRequest request,
        CancellationToken cancellationToken)
    {
        await accountRecoveryService.RequestTenantPasswordResetAsync(request.Email, cancellationToken);
        return Accepted(new ForgotPasswordResponse("If the account is eligible, a password reset email has been sent."));
    }

    [AllowAnonymous]
    [EnableRateLimiting("account-recovery-token")]
    [HttpPost("api/v1/auth/reset-password")]
    public async Task<IActionResult> TenantResetPassword(CompletePasswordRequest request, CancellationToken cancellationToken) =>
        PasswordResult(await accountRecoveryService.ResetTenantPasswordAsync(request.UserId, request.Token, request.NewPassword, cancellationToken));

    [AllowAnonymous]
    [EnableRateLimiting("account-recovery-token")]
    [HttpPost("api/v1/auth/initial-password-setup")]
    public async Task<IActionResult> InitialPasswordSetup(CompletePasswordRequest request, CancellationToken cancellationToken) =>
        PasswordResult(await accountRecoveryService.CompleteInitialPasswordSetupAsync(request.UserId, request.Token, request.NewPassword, cancellationToken));

    [AllowAnonymous]
    [EnableRateLimiting("account-recovery-forgot")]
    [HttpPost("api/v1/platform/auth/forgot-password")]
    public async Task<ActionResult<ForgotPasswordResponse>> PlatformForgotPassword(
        ForgotPasswordRequest request,
        CancellationToken cancellationToken)
    {
        await accountRecoveryService.RequestPlatformPasswordResetAsync(request.Email, cancellationToken);
        return Accepted(new ForgotPasswordResponse("If the account is eligible, a password reset email has been sent."));
    }

    [AllowAnonymous]
    [EnableRateLimiting("account-recovery-token")]
    [HttpPost("api/v1/platform/auth/reset-password")]
    public async Task<IActionResult> PlatformResetPassword(CompletePasswordRequest request, CancellationToken cancellationToken) =>
        PasswordResult(await accountRecoveryService.ResetPlatformPasswordAsync(request.UserId, request.Token, request.NewPassword, cancellationToken));

    private IActionResult PasswordResult(AccountRecoveryResult result)
    {
        if (result.Succeeded) return NoContent();
        if (result.IsConflict)
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Password setup is already complete.",
                Detail = result.Errors.FirstOrDefault(),
            });
        }
        foreach (string error in result.Errors)
        {
            ModelState.AddModelError("newPassword", error);
        }
        return ValidationProblem(ModelState);
    }
}
