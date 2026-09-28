using JobBoard.Application.Abstractions.Authentication;
using JobBoard.Application.Authentication;

namespace JobBoard.Infrastructure.Authentication;

public sealed class InMemoryUserAccountRepository : IUserAccountRepository
{
    private readonly IReadOnlyDictionary<string, UserAccount> _accounts;

    public InMemoryUserAccountRepository(IPasswordHasher passwordHasher)
    {
        ArgumentNullException.ThrowIfNull(passwordHasher);

        const string demoPassword = "JobBoard@123";
        var accounts = new[]
        {
            new UserAccount(
                20,
                "Nguyễn Minh An",
                "candidate@jobboard.vn",
                passwordHasher.Hash(demoPassword),
                AccountRole.Candidate),
            new UserAccount(
                1,
                "Trần Bình",
                "employer@jobboard.vn",
                passwordHasher.Hash(demoPassword),
                AccountRole.Employer,
                CompanyId: 1,
                CompanyName: "Nova Technology"),
            new UserAccount(
                100,
                "Quản trị viên",
                "admin@jobboard.vn",
                passwordHasher.Hash(demoPassword),
                AccountRole.Admin)
        };

        _accounts = accounts.ToDictionary(
            account => account.Email,
            StringComparer.OrdinalIgnoreCase);
    }

    public Task<UserAccount?> FindByEmailAsync(
        string normalizedEmail,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _accounts.TryGetValue(normalizedEmail, out var account);
        return Task.FromResult(account);
    }
}
