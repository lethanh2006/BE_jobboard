namespace JobBoard.Api.Contracts.Candidates;

public sealed record UpdateCandidateProfileRequest(
    string Email,
    string? Bio,
    decimal? DesiredSalaryMin,
    decimal? DesiredSalaryMax,
    IReadOnlyCollection<string> Skills);
