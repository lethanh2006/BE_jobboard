using JobBoard.Application.Abstractions.Persistence;
using JobBoard.Application.Exceptions;
using JobBoard.Domain.Entities;

namespace JobBoard.Application.Candidates;

public sealed class CandidateProfileService(ICandidateRepository candidates)
{
    private readonly ICandidateRepository _candidates =
        candidates ?? throw new ArgumentNullException(nameof(candidates));

    public async Task<CandidateProfileDto> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var candidate = await GetCandidateAsync(id, cancellationToken);
        return Map(candidate);
    }

    public async Task<CandidateProfileDto> UpdateAsync(
        int id,
        UpdateCandidateProfileCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        var candidate = await GetCandidateAsync(id, cancellationToken);
        var salary = CreateSalaryRange(command.DesiredSalaryMin, command.DesiredSalaryMax);
        var skills = command.Skills.Select(skill => new Skill(skill)).ToList();

        candidate.UpdateProfile(command.Email, command.Bio, salary, skills);
        await _candidates.UpdateAsync(candidate, cancellationToken);

        return Map(candidate);
    }

    private async Task<Candidate> GetCandidateAsync(int id, CancellationToken cancellationToken) =>
        await _candidates.GetByIdAsync(id, cancellationToken)
            ?? throw new ResourceNotFoundException($"Không tìm thấy ứng viên có mã {id}.");

    private static SalaryRange? CreateSalaryRange(decimal? min, decimal? max)
    {
        if (min is null && max is null)
        {
            return null;
        }

        if (min is null || max is null)
        {
            throw new ArgumentException("Khoảng lương mong muốn phải có đủ mức tối thiểu và tối đa.");
        }

        return new SalaryRange(min.Value, max.Value);
    }

    private static CandidateProfileDto Map(Candidate candidate) =>
        new(
            candidate.Id,
            candidate.Name,
            candidate.Email,
            candidate.Bio,
            candidate.DesiredSalary?.Min,
            candidate.DesiredSalary?.Max,
            candidate.Skills.Select(skill => skill.Name).Order(StringComparer.OrdinalIgnoreCase).ToList());
}
