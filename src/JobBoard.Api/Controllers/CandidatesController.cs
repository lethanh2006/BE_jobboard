using JobBoard.Api.Contracts.Candidates;
using JobBoard.Application.Applications;
using JobBoard.Application.Candidates;
using Microsoft.AspNetCore.Mvc;

namespace JobBoard.Api.Controllers;

[ApiController]
[Route("api/candidates")]
public sealed class CandidatesController(
    ApplicationQueryService applicationQueryService,
    CandidateProfileService candidateProfileService) : ControllerBase
{
    [HttpGet("{candidateId:int}/profile")]
    [ProducesResponseType<CandidateProfileDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<CandidateProfileDto>> GetProfile(
        int candidateId,
        CancellationToken cancellationToken)
    {
        return Ok(await candidateProfileService.GetByIdAsync(candidateId, cancellationToken));
    }

    [HttpPut("{candidateId:int}/profile")]
    [ProducesResponseType<CandidateProfileDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<CandidateProfileDto>> UpdateProfile(
        int candidateId,
        UpdateCandidateProfileRequest request,
        CancellationToken cancellationToken)
    {
        var profile = await candidateProfileService.UpdateAsync(
            candidateId,
            new UpdateCandidateProfileCommand(
                request.Email,
                request.Bio,
                request.DesiredSalaryMin,
                request.DesiredSalaryMax,
                request.Skills),
            cancellationToken);

        return Ok(profile);
    }

    [HttpGet("{candidateId:int}/applications")]
    [ProducesResponseType<IReadOnlyList<ApplicationDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ApplicationDto>>> GetApplications(
        int candidateId,
        CancellationToken cancellationToken)
    {
        var applications = await applicationQueryService.GetByCandidateIdAsync(
            candidateId,
            cancellationToken);

        return Ok(applications);
    }
}
