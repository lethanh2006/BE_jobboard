using JobBoard.Api.Contracts.Applications;
using JobBoard.Application.Applications;
using JobBoard.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace JobBoard.Api.Controllers;

[ApiController]
[Route("api/applications")]
public sealed class ApplicationsController(ApplicationService applicationService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<JobApplication>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<JobApplication>> Apply(
        ApplyRequest request,
        CancellationToken cancellationToken)
    {
        var application = await applicationService.ApplyAsync(
            request.CandidateId,
            request.JobId,
            cancellationToken);

        return Created($"/api/applications/{application.Id}", application);
    }

    [HttpPatch("{id:int}/status")]
    [ProducesResponseType<JobApplication>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<JobApplication>> ChangeStatus(
        int id,
        ChangeApplicationStatusRequest request,
        CancellationToken cancellationToken)
    {
        var company = new Company(request.CompanyId, $"Công ty #{request.CompanyId}");
        var manager = new Employer(1, "Nhà tuyển dụng", "employer@jobboard.vn", company);
        var application = await applicationService.ChangeStatusAsync(
            id,
            request.Status,
            manager,
            cancellationToken);

        return Ok(application);
    }
}
