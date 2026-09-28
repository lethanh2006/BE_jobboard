using JobBoard.Application.Abstractions.Authentication;
using JobBoard.Application.Authentication;
using JobBoard.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace JobBoard.Infrastructure.Persistence.Database;

public sealed class PostgresUserAccountRepository(JobBoardDbContext dbContext)
    : IUserAccountRepository, IAccountRegistrationRepository
{
    private readonly JobBoardDbContext _dbContext =
        dbContext ?? throw new ArgumentNullException(nameof(dbContext));

    public async Task<UserAccount?> FindByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.UserAccounts
            .AsNoTracking()
            .SingleOrDefaultAsync(account => account.Id == id, cancellationToken);

        return record?.ToDomain();
    }

    public async Task<UserAccount?> FindByEmailAsync(
        string normalizedEmail,
        CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.UserAccounts
            .AsNoTracking()
            .SingleOrDefaultAsync(
                account => account.Email == normalizedEmail,
                cancellationToken);

        return record?.ToDomain();
    }

    public async Task<int> GetNextUserIdAsync(CancellationToken cancellationToken = default) =>
        await _dbContext.Database
            .SqlQueryRaw<int>("SELECT nextval('user_ids')::integer AS \"Value\"")
            .SingleAsync(cancellationToken);

    public Task<bool> EmailExistsAsync(
        string normalizedEmail,
        CancellationToken cancellationToken = default) =>
        _dbContext.UserAccounts
            .AsNoTracking()
            .AnyAsync(account => account.Email == normalizedEmail, cancellationToken);

    public async Task<bool> RegisterCandidateAsync(
        UserAccount account,
        Candidate candidate,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(account);
        ArgumentNullException.ThrowIfNull(candidate);
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        _dbContext.UserAccounts.Add(account.ToRecord());
        _dbContext.Candidates.Add(candidate.ToRecord());

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is PostgresException
            { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }
    }
}
