using JobBoard.Application.Authentication;
using JobBoard.Domain.Entities;
using JobBoard.Infrastructure.Authentication;
using JobBoard.Infrastructure.Persistence;

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

    [Fact]
    public async Task RegisterCandidateAsync_PersistsAccountAndProfileTogether()
    {
        var hasher = new Pbkdf2PasswordHasher();
        var candidates = new InMemoryCandidateRepository(seedDemoData: true);
        var repository = new InMemoryUserAccountRepository(hasher, candidates);
        var id = await repository.GetNextUserIdAsync();
        var account = new UserAccount(
            id,
            "Trần Thu Hà",
            "ha@example.com",
            hasher.Hash("StrongPass@123"),
            AccountRole.Candidate);
        var candidate = new Candidate(id, account.Name, account.Email);

        var registered = await repository.RegisterCandidateAsync(account, candidate);

        Assert.True(registered);
        Assert.Equal(account, await repository.FindByEmailAsync(account.Email));
        Assert.Equal(candidate.Id, (await candidates.GetByIdAsync(candidate.Id))?.Id);
        Assert.False(await repository.RegisterCandidateAsync(account, candidate));
    }

    [Fact]
    public async Task TryUpdatePasswordHashAsync_OnlyUpdatesMatchingPasswordHash()
    {
        var hasher = new Pbkdf2PasswordHasher();
        var repository = new InMemoryUserAccountRepository(hasher);
        var account = await repository.FindByIdAsync(20);
        Assert.NotNull(account);

        var staleUpdate = await repository.TryUpdatePasswordHashAsync(
            account.Id,
            "stale-hash",
            "new-hash");
        var successfulUpdate = await repository.TryUpdatePasswordHashAsync(
            account.Id,
            account.PasswordHash,
            "new-hash");

        Assert.False(staleUpdate);
        Assert.True(successfulUpdate);
        Assert.Equal("new-hash", (await repository.FindByIdAsync(account.Id))?.PasswordHash);
    }
}
