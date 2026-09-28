using JobBoard.Application.Abstractions.Persistence;
using JobBoard.Application.Exceptions;

namespace JobBoard.Application.Jobs;

public sealed class JobDetailService(IJobRepository jobs)
{
    private readonly IJobRepository _jobs = jobs ?? throw new ArgumentNullException(nameof(jobs));

    public async Task<JobDetailDto> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var job = await _jobs.GetByIdAsync(id, cancellationToken)
            ?? throw new ResourceNotFoundException($"Không tìm thấy tin tuyển dụng có mã {id}.");

        return new JobDetailDto(
            job.Id,
            job.Title,
            job.CompanyId,
            job.Company?.Name ?? $"Công ty #{job.CompanyId}",
            job.Location,
            job.Salary.Min,
            job.Salary.Max,
            job.Level,
            job.WorkMode,
            job.Category,
            job.Summary,
            job.Description,
            job.Requirements,
            job.Benefits,
            job.PostedAt,
            job.Deadline,
            job.Featured,
            job.Skills.Select(skill => skill.Name).Order(StringComparer.OrdinalIgnoreCase).ToList());
    }
}
