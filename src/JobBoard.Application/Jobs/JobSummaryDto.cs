namespace JobBoard.Application.Jobs;

public sealed record JobSummaryDto(
    int Id,
    string Title,
    int CompanyId,
    decimal SalaryMin,
    decimal SalaryMax,
    DateTime Deadline,
    IReadOnlyList<string> Skills);
