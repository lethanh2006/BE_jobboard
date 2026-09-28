using JobBoard.Domain.Entities;

namespace JobBoard.Application.Jobs;

public sealed record JobDetailDto(
    int Id,
    string Title,
    int CompanyId,
    string Company,
    string Location,
    decimal SalaryMin,
    decimal SalaryMax,
    JobLevel Level,
    WorkMode WorkMode,
    string Category,
    string Summary,
    IReadOnlyList<string> Description,
    IReadOnlyList<string> Requirements,
    IReadOnlyList<string> Benefits,
    DateTime PostedAt,
    DateTime Deadline,
    bool Featured,
    IReadOnlyList<string> Skills);
