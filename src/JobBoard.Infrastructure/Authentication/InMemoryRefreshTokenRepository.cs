using JobBoard.Application.Abstractions.Authentication;
using JobBoard.Application.Authentication;

namespace JobBoard.Infrastructure.Authentication;

public sealed class InMemoryRefreshTokenRepository : IRefreshTokenRepository
{
    private readonly Dictionary<Guid, RefreshTokenSession> _sessions = [];
    private readonly object _syncRoot = new();

    public Task<RefreshTokenSession?> FindByHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_syncRoot)
        {
            var session = _sessions.Values.SingleOrDefault(item => item.TokenHash == tokenHash);
            return Task.FromResult(session is null ? null : Clone(session));
        }
    }

    public Task AddAsync(
        RefreshTokenSession session,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        cancellationToken.ThrowIfCancellationRequested();
        lock (_syncRoot)
        {
            _sessions.Add(session.Id, Clone(session));
        }

        return Task.CompletedTask;
    }

    public Task<bool> TryRotateAsync(
        Guid currentSessionId,
        string replacementTokenHash,
        RefreshTokenSession replacement,
        DateTime revokedAt,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(replacement);
        cancellationToken.ThrowIfCancellationRequested();
        lock (_syncRoot)
        {
            if (!_sessions.TryGetValue(currentSessionId, out var current) ||
                current.RevokedAt is not null)
            {
                return Task.FromResult(false);
            }

            current.Revoke(revokedAt, replacementTokenHash);
            _sessions.Add(replacement.Id, Clone(replacement));
            return Task.FromResult(true);
        }
    }

    public Task RevokeByHashAsync(
        string tokenHash,
        DateTime revokedAt,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_syncRoot)
        {
            _sessions.Values.SingleOrDefault(session => session.TokenHash == tokenHash)?.Revoke(revokedAt);
        }

        return Task.CompletedTask;
    }

    public Task RevokeFamilyAsync(
        Guid familyId,
        DateTime revokedAt,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_syncRoot)
        {
            foreach (var session in _sessions.Values.Where(session => session.FamilyId == familyId))
            {
                session.Revoke(revokedAt);
            }
        }

        return Task.CompletedTask;
    }

    private static RefreshTokenSession Clone(RefreshTokenSession session) =>
        RefreshTokenSession.Restore(
            session.Id,
            session.UserId,
            session.FamilyId,
            session.TokenHash,
            session.CreatedAt,
            session.ExpiresAt,
            session.RevokedAt,
            session.ReplacedByTokenHash);
}
