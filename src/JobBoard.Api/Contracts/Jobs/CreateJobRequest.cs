using JobBoard.Domain.Entities;

namespace JobBoard.Api.Contracts.Jobs;

public sealed record CreateJobRequest(
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
