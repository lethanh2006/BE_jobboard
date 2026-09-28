using JobBoard.Api.Authentication;
using JobBoard.Api.Contracts.Authentication;
using JobBoard.Application.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JobBoard.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(LoginService loginService) : ControllerBase
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
