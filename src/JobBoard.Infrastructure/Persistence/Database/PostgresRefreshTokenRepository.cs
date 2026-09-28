using JobBoard.Application.Abstractions.Authentication;
using JobBoard.Application.Authentication;
using Microsoft.EntityFrameworkCore;

namespace JobBoard.Infrastructure.Persistence.Database;

public sealed class PostgresRefreshTokenRepository(JobBoardDbContext dbContext)
    : IRefreshTokenRepository
{
    private readonly JobBoardDbContext _dbContext =
        dbContext ?? throw new ArgumentNullException(nameof(dbContext));

    public async Task<RefreshTokenSession?> FindByHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.RefreshTokens
            .AsNoTracking()
            .SingleOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);

        return record?.ToDomain();
    }

    public async Task AddAsync(
        RefreshTokenSession session,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        _dbContext.RefreshTokens.Add(session.ToRecord());
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> TryRotateAsync(
        Guid currentSessionId,
        string replacementTokenHash,
        RefreshTokenSession replacement,
        DateTime revokedAt,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(replacement);
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        var updatedRows = await _dbContext.RefreshTokens
            .Where(token => token.Id == currentSessionId && token.RevokedAt == null)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(token => token.RevokedAt, revokedAt)
                    .SetProperty(token => token.ReplacedByTokenHash, replacementTokenHash),
                cancellationToken);

        if (updatedRows == 0)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        _dbContext.RefreshTokens.Add(replacement.ToRecord());
        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public Task RevokeByHashAsync(
        string tokenHash,
        DateTime revokedAt,
        CancellationToken cancellationToken = default) =>
        _dbContext.RefreshTokens
            .Where(token => token.TokenHash == tokenHash && token.RevokedAt == null)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(token => token.RevokedAt, revokedAt),
                cancellationToken);

    public Task RevokeFamilyAsync(
        Guid familyId,
        DateTime revokedAt,
        CancellationToken cancellationToken = default) =>
        _dbContext.RefreshTokens
            .Where(token => token.FamilyId == familyId && token.RevokedAt == null)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(token => token.RevokedAt, revokedAt),
                cancellationToken);

    public Task RevokeByUserAsync(
        int userId,
        DateTime revokedAt,
        CancellationToken cancellationToken = default) =>
        _dbContext.RefreshTokens
            .Where(token => token.UserId == userId && token.RevokedAt == null)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(token => token.RevokedAt, revokedAt),
                cancellationToken);
}
