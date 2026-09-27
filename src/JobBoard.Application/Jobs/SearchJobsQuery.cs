namespace JobBoard.Application.Jobs;

public sealed record SearchJobsQuery(
    string? Keyword = null,
    decimal? MinimumSalary = null,
    IReadOnlyCollection<string>? Skills = null,
    int Page = 1,
    int PageSize = 20);
