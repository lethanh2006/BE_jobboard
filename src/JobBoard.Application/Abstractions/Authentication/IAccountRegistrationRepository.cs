using JobBoard.Application.Authentication;
using JobBoard.Domain.Entities;

namespace JobBoard.Application.Abstractions.Authentication;

public interface IAccountRegistrationRepository
{
    Task<int> GetNextUserIdAsync(CancellationToken cancellationToken = default);

    Task<bool> EmailExistsAsync(
        string normalizedEmail,
        CancellationToken cancellationToken = default);

    Task<bool> RegisterCandidateAsync(
        UserAccount account,
        Candidate candidate,
        CancellationToken cancellationToken = default);
}
