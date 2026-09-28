using JobBoard.Application.Authentication;

namespace JobBoard.Application.Abstractions.Authentication;

public interface IRefreshTokenRepository
{
    Task<RefreshTokenSession?> FindByHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        RefreshTokenSession session,
        CancellationToken cancellationToken = default);

    Task<bool> TryRotateAsync(
        Guid currentSessionId,
        string replacementTokenHash,
        RefreshTokenSession replacement,
        DateTime revokedAt,
        CancellationToken cancellationToken = default);

    Task RevokeByHashAsync(
        string tokenHash,
        DateTime revokedAt,
        CancellationToken cancellationToken = default);

    Task RevokeFamilyAsync(
        Guid familyId,
        DateTime revokedAt,
        CancellationToken cancellationToken = default);
}
