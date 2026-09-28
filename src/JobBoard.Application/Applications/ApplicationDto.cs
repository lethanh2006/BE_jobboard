using JobBoard.Domain.Entities;

namespace JobBoard.Application.Applications;

public sealed record ApplicationDto(
    int Id,
    int JobId,
    int CandidateId,
    string CandidateName,
    string JobTitle,
    string Company,
    string Location,
    ApplicationStatus Status,
    DateTime AppliedAt);
