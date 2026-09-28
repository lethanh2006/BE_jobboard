using JobBoard.Application.Abstractions.Authentication;
using JobBoard.Application.Authentication;
using Microsoft.EntityFrameworkCore;

namespace JobBoard.Infrastructure.Persistence.Database;

public sealed class PostgresUserAccountRepository(JobBoardDbContext dbContext)
    : IUserAccountRepository
{
    private readonly JobBoardDbContext _dbContext =
        dbContext ?? throw new ArgumentNullException(nameof(dbContext));

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
}
