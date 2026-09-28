namespace JobBoard.Application.Candidates;

public sealed record CandidateProfileDto(
    int Id,
    string Name,
    string Email,
    string? Bio,
    decimal? DesiredSalaryMin,
    decimal? DesiredSalaryMax,
    IReadOnlyList<string> Skills);
