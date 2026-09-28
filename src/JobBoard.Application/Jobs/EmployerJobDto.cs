using JobBoard.Domain.Entities;

namespace JobBoard.Application.Jobs;

public sealed record EmployerJobDto(
    int Id,
    string Title,
    JobStatus Status,
    int Applications,
    DateTime Deadline,
    DateTime PostedAt);
