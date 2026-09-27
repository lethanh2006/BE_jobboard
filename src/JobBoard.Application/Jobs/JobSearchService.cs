using JobBoard.Application.Abstractions.Persistence;
using JobBoard.Application.Common;
using JobBoard.Domain.Entities;

namespace JobBoard.Application.Jobs;

public sealed class JobSearchService(IJobRepository jobs)
{
    private readonly IJobRepository _jobs = jobs ?? throw new ArgumentNullException(nameof(jobs));

    public async Task<PagedResult<JobSummaryDto>> SearchAsync(
        SearchJobsQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.Page <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(query), "Số trang phải lớn hơn 0.");
        }

        if (query.PageSize is <= 0 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(query), "Kích thước trang phải từ 1 đến 100.");
        }

        if (query.MinimumSalary < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(query), "Mức lương lọc không được âm.");
        }

        var keyword = query.Keyword?.Trim();
        var requiredSkills = new HashSet<string>(
            (query.Skills ?? [])
                .Where(skill => !string.IsNullOrWhiteSpace(skill))
                .Select(skill => skill.Trim()),
            StringComparer.OrdinalIgnoreCase);

        Func<Job, bool> matchesKeyword = job =>
            string.IsNullOrEmpty(keyword) || job.Title.Contains(keyword, StringComparison.OrdinalIgnoreCase);
        Func<Job, bool> matchesSalary = job =>
            query.MinimumSalary is null || job.Salary.Min >= query.MinimumSalary.Value;
        Func<Job, bool> matchesSkills = job =>
            requiredSkills.Count == 0 || requiredSkills.All(requiredSkill =>
                job.Skills.Any(skill => string.Equals(skill.Name, requiredSkill, StringComparison.OrdinalIgnoreCase)));

        var jobs = await _jobs.GetAllAsync(cancellationToken);
        var filteredJobs = jobs
            .Where(job => job.Status == JobStatus.Published && !job.IsExpired)
            .Where(job => matchesKeyword(job) && matchesSalary(job) && matchesSkills(job))
            .OrderByDescending(job => job.Salary.Max)
            .ThenBy(job => job.Id)
            .ToList();

        var items = filteredJobs
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(MapSummary)
            .ToList();

        return new PagedResult<JobSummaryDto>(items, query.Page, query.PageSize, filteredJobs.Count);
    }

    private static JobSummaryDto MapSummary(Job job) =>
        new(
            job.Id,
            job.Title,
            job.CompanyId,
            job.Salary.Min,
            job.Salary.Max,
            job.Deadline,
            job.Skills.Select(skill => skill.Name).Order(StringComparer.OrdinalIgnoreCase).ToList());
}
