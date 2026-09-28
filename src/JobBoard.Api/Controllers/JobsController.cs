using JobBoard.Application.Common;
using JobBoard.Application.Jobs;
using JobBoard.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace JobBoard.Api.Controllers;

[ApiController]
[Route("api/jobs")]
public sealed class JobsController(
    JobSearchService searchService,
    JobDetailService detailService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PagedResult<JobSummaryDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<JobSummaryDto>>> Search(
        [FromQuery] string? keyword,
        [FromQuery] string? location,
        [FromQuery] string? category,
        [FromQuery] JobLevel? level,
        [FromQuery] decimal? minimumSalary,
        [FromQuery] string[]? skills,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] JobSortOrder sort = JobSortOrder.Newest,
        CancellationToken cancellationToken = default)
    {
        var result = await searchService.SearchAsync(
            new SearchJobsQuery(
                keyword,
                minimumSalary,
                skills,
                page,
                pageSize,
                location,
                category,
                level,
                sort),
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType<JobDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<JobDetailDto>> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        var result = await detailService.GetByIdAsync(id, cancellationToken);
        return Ok(result);
    }
}
