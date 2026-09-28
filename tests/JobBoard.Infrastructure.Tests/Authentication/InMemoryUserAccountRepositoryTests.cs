using JobBoard.Application.Authentication;
using JobBoard.Infrastructure.Authentication;

namespace JobBoard.Infrastructure.Tests.Authentication;

public sealed class InMemoryUserAccountRepositoryTests
{
    [Fact]
    public async Task FindByEmailAsync_ReturnsSeededAccountCaseInsensitively()
    {
        var hasher = new Pbkdf2PasswordHasher();
        var repository = new InMemoryUserAccountRepository(hasher);

        var account = await repository.FindByEmailAsync("EMPLOYER@JOBBOARD.VN");

        Assert.NotNull(account);
        Assert.Equal(AccountRole.Employer, account.Role);
        Assert.Equal(1, account.CompanyId);
        Assert.True(hasher.Verify("JobBoard@123", account.PasswordHash));
    }
}
