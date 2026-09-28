using JobBoard.Api.Authentication;
using JobBoard.Api.Contracts.Applications;
using JobBoard.Application.Applications;
using JobBoard.Application.Authentication;
using JobBoard.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JobBoard.Api.Controllers;

[ApiController]
[Route("api/applications")]
public sealed class ApplicationsController(ApplicationService applicationService) : ControllerBase
{
    [HttpPost]
    [Authorize(Roles = nameof(AccountRole.Candidate))]
    [ProducesResponseType<JobApplication>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<JobApplication>> Apply(
        ApplyRequest request,
        CancellationToken cancellationToken)
    {
        var application = await applicationService.ApplyAsync(
            User.GetRequiredUserId(),
            request.JobId,
            cancellationToken);

        return Created($"/api/applications/{application.Id}", application);
    }

    [HttpPatch("{id:int}/status")]
    [Authorize(Roles = nameof(AccountRole.Employer))]
    [ProducesResponseType<JobApplication>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<JobApplication>> ChangeStatus(
        int id,
        ChangeApplicationStatusRequest request,
        CancellationToken cancellationToken)
    {
        var companyId = User.GetRequiredCompanyId();
        var company = new Company(companyId, $"Công ty #{companyId}");
        var manager = new Employer(
            User.GetRequiredUserId(),
            User.GetRequiredClaim(JobBoardClaimTypes.Name),
            User.GetRequiredClaim(JobBoardClaimTypes.Email),
            company);
        var application = await applicationService.ChangeStatusAsync(
            id,
            request.Status,
            manager,
            cancellationToken);

        return Ok(application);
    }
}
