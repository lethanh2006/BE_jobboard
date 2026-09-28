using JobBoard.Domain.Entities;

namespace JobBoard.Application.Jobs;

public sealed record CreateJobCommand(
    int CompanyId,
    string CompanyName,
    string Title,
    string Location,
    decimal SalaryMin,
    decimal SalaryMax,
    DateTime Deadline,
    JobLevel Level,
    WorkMode WorkMode,
    string Category,
    string Summary,
    IReadOnlyCollection<string> Skills);
