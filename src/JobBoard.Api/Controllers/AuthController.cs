using JobBoard.Api.Authentication;
using JobBoard.Api.Contracts.Authentication;
using JobBoard.Application.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JobBoard.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(
    LoginService loginService,
    SessionService sessionService,
    RegistrationService registrationService,
    PasswordChangeService passwordChangeService) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType<LoginResult>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResult>> Login(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await loginService.LoginAsync(request.Email, request.Password, cancellationToken));
    }

    [AllowAnonymous]
    [HttpPost("register/candidate")]
    [ProducesResponseType<LoginResult>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<LoginResult>> RegisterCandidate(
        RegisterCandidateRequest request,
        CancellationToken cancellationToken)
    {
        var result = await registrationService.RegisterCandidateAsync(
            request.Name,
            request.Email,
            request.Password,
            cancellationToken);

        return Created($"/api/candidates/{result.User.Id}/profile", result);
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    [ProducesResponseType<LoginResult>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResult>> Refresh(
        RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await sessionService.RefreshAsync(request.RefreshToken, cancellationToken));
    }

    [AllowAnonymous]
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(
        RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        await sessionService.LogoutAsync(request.RefreshToken, cancellationToken);
        return NoContent();
    }

    [Authorize]
    [HttpPut("password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword(
        ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        await passwordChangeService.ChangeAsync(
            User.GetRequiredUserId(),
            request.CurrentPassword,
            request.NewPassword,
            cancellationToken);

        return NoContent();
    }

    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType<AuthenticatedUserDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public ActionResult<AuthenticatedUserDto> GetCurrentUser()
    {
        var role = Enum.Parse<AccountRole>(
            User.GetRequiredClaim(JobBoardClaimTypes.Role),
            ignoreCase: false);
        var companyId = User.FindFirst(JobBoardClaimTypes.CompanyId) is { Value: var value }
            ? int.Parse(value, System.Globalization.CultureInfo.InvariantCulture)
            : null as int?;

        return Ok(new AuthenticatedUserDto(
            User.GetRequiredUserId(),
            User.GetRequiredClaim(JobBoardClaimTypes.Name),
            User.GetRequiredClaim(JobBoardClaimTypes.Email),
            role,
            companyId,
            User.FindFirst(JobBoardClaimTypes.CompanyName)?.Value));
    }
}
