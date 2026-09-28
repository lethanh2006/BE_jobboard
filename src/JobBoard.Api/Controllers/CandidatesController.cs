using JobBoard.Application.Applications;
using Microsoft.AspNetCore.Mvc;

namespace JobBoard.Api.Controllers;

[ApiController]
[Route("api/candidates")]
public sealed class CandidatesController(ApplicationQueryService applicationQueryService) : ControllerBase
{
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
