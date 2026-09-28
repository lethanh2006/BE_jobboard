using JobBoard.Api.Authentication;
using JobBoard.Api.Contracts.Jobs;
using JobBoard.Application.Applications;
using JobBoard.Application.Authentication;
using JobBoard.Application.Jobs;
using JobBoard.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JobBoard.Api.Controllers;

[ApiController]
[Authorize(Roles = nameof(AccountRole.Employer))]
[Route("api/employers/{companyId:int}")]
public sealed class EmployersController(
    JobManagementService jobManagementService,
    ApplicationQueryService applicationQueryService) : ControllerBase
{
    [HttpGet("jobs")]
    [ProducesResponseType<IReadOnlyList<EmployerJobDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<EmployerJobDto>>> GetJobs(
        int companyId,
        CancellationToken cancellationToken)
    {
        if (!CanManageCompany(companyId))
        {
            return Forbid();
        }

        return Ok(await jobManagementService.GetByCompanyIdAsync(companyId, cancellationToken));
    }

    [HttpPost("jobs")]
    [ProducesResponseType<EmployerJobDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<EmployerJobDto>> CreateJob(
        int companyId,
        CreateJobRequest request,
        CancellationToken cancellationToken)
    {
        if (!CanManageCompany(companyId))
        {
            return Forbid();
        }

        var job = await jobManagementService.CreateDraftAsync(
            new CreateJobCommand(
                companyId,
                User.GetRequiredClaim(JobBoardClaimTypes.CompanyName),
                request.Title,
                request.Location,
                request.SalaryMin,
                request.SalaryMax,
                request.Deadline,
                request.Level,
                request.WorkMode,
                request.Category,
                request.Summary,
                request.Skills),
            cancellationToken);

        return Created($"/api/jobs/{job.Id}", job);
    }

    [HttpPost("jobs/{jobId:int}/publish")]
    public async Task<ActionResult<EmployerJobDto>> PublishJob(
        int companyId,
        int jobId,
        CancellationToken cancellationToken)
    {
        if (!CanManageCompany(companyId))
        {
            return Forbid();
        }

        return Ok(await jobManagementService.PublishAsync(
            jobId,
            CreateManager(companyId),
            cancellationToken));
    }

    [HttpPost("jobs/{jobId:int}/close")]
    public async Task<ActionResult<EmployerJobDto>> CloseJob(
        int companyId,
        int jobId,
        CancellationToken cancellationToken)
    {
        if (!CanManageCompany(companyId))
        {
            return Forbid();
        }

        return Ok(await jobManagementService.CloseAsync(
            jobId,
            CreateManager(companyId),
            cancellationToken));
    }

    [HttpGet("applications")]
    [ProducesResponseType<IReadOnlyList<ApplicationDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ApplicationDto>>> GetApplications(
        int companyId,
        CancellationToken cancellationToken)
    {
        if (!CanManageCompany(companyId))
        {
            return Forbid();
        }

        return Ok(await applicationQueryService.GetByCompanyIdAsync(companyId, cancellationToken));
    }

    private bool CanManageCompany(int companyId) => User.GetRequiredCompanyId() == companyId;

    private Employer CreateManager(int companyId) =>
        new(
            User.GetRequiredUserId(),
            User.GetRequiredClaim(JobBoardClaimTypes.Name),
            User.GetRequiredClaim(JobBoardClaimTypes.Email),
            new Company(companyId, $"Công ty #{companyId}"));
}
