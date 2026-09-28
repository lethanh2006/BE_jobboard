using JobBoard.Domain.Entities;

namespace JobBoard.Application.Jobs;

public sealed record SearchJobsQuery(
    string? Keyword = null,
    decimal? MinimumSalary = null,
    IReadOnlyCollection<string>? Skills = null,
    int Page = 1,
    int PageSize = 20,
    string? Location = null,
    string? Category = null,
    JobLevel? Level = null,
    JobSortOrder Sort = JobSortOrder.Newest);

public enum JobSortOrder
{
    Newest,
    SalaryDescending
}
