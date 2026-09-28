namespace JobBoard.Application.Candidates;

public sealed record UpdateCandidateProfileCommand(
    string Email,
    string? Bio,
    decimal? DesiredSalaryMin,
    decimal? DesiredSalaryMax,
    IReadOnlyCollection<string> Skills);
