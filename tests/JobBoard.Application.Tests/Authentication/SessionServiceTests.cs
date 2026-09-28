using JobBoard.Application.Abstractions.Authentication;
using JobBoard.Application.Authentication;
using JobBoard.Application.Exceptions;
using JobBoard.Domain.Abstractions;

namespace JobBoard.Application.Tests.Authentication;

public sealed class SessionServiceTests
{
    private static readonly DateTime Now = new(2026, 9, 28, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task RefreshAsync_WithActiveToken_RotatesTokenAndReturnsNewAccessToken()
    {
        var account = CreateAccount();
        var current = RefreshTokenSession.Create(
            account.Id,
            Guid.NewGuid(),
            "old-token-hash",
            Now.AddDays(-1),
            Now.AddDays(6));
        var repository = new StubRefreshTokenRepository(current);
        var service = CreateService(account, repository);

        var result = await service.RefreshAsync("old-token");

        Assert.Equal("new-access-token", result.AccessToken);
        Assert.Equal("new-refresh-token", result.RefreshToken);
        Assert.Equal(1, repository.RotateCount);
        Assert.Equal(current.FamilyId, repository.Replacement?.FamilyId);
    }

    [Fact]
    public async Task RefreshAsync_WhenRevokedTokenIsReused_RevokesWholeFamily()
    {
        var account = CreateAccount();
        var current = RefreshTokenSession.Create(
            account.Id,
            Guid.NewGuid(),
            "old-token-hash",
            Now.AddDays(-1),
            Now.AddDays(6));
        current.Revoke(Now.AddMinutes(-1), "new-token-hash");
        var repository = new StubRefreshTokenRepository(current);
        var service = CreateService(account, repository);

        await Assert.ThrowsAsync<InvalidRefreshTokenException>(
            () => service.RefreshAsync("old-token"));

        Assert.Equal(current.FamilyId, repository.RevokedFamilyId);
    }

    [Fact]
    public async Task LogoutAsync_WithRefreshToken_RevokesCurrentSession()
    {
        var account = CreateAccount();
        var repository = new StubRefreshTokenRepository();
        var service = CreateService(account, repository);

        await service.LogoutAsync("old-token");

        Assert.Equal("old-token-hash", repository.RevokedTokenHash);
    }

    private static SessionService CreateService(
        UserAccount account,
        StubRefreshTokenRepository repository) =>
        new(
            new StubAccountRepository(account),
            repository,
            new StubRefreshTokenGenerator(),
            new StubAccessTokenGenerator(),
            new FixedClock(Now));

    private static UserAccount CreateAccount() =>
        new(
            20,
            "Nguyễn Minh An",
            "candidate@jobboard.vn",
            "stored-hash",
            AccountRole.Candidate);

    private sealed class StubAccountRepository(UserAccount account) : IUserAccountRepository
    {
        public Task<UserAccount?> FindByIdAsync(
            int id,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<UserAccount?>(id == account.Id ? account : null);

        public Task<UserAccount?> FindByEmailAsync(
            string normalizedEmail,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<UserAccount?>(account);
    }

    private sealed class StubRefreshTokenRepository(params RefreshTokenSession[] sessions)
        : IRefreshTokenRepository
    {
        public int RotateCount { get; private set; }

        public RefreshTokenSession? Replacement { get; private set; }

        public Guid? RevokedFamilyId { get; private set; }

        public string? RevokedTokenHash { get; private set; }

        public Task<RefreshTokenSession?> FindByHashAsync(
            string tokenHash,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(sessions.SingleOrDefault(session => session.TokenHash == tokenHash));

        public Task AddAsync(
            RefreshTokenSession session,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<bool> TryRotateAsync(
            Guid currentSessionId,
            string replacementTokenHash,
            RefreshTokenSession replacement,
            DateTime revokedAt,
            CancellationToken cancellationToken = default)
        {
            RotateCount++;
            Replacement = replacement;
            return Task.FromResult(true);
        }

        public Task RevokeByHashAsync(
            string tokenHash,
            DateTime revokedAt,
            CancellationToken cancellationToken = default)
        {
            RevokedTokenHash = tokenHash;
            return Task.CompletedTask;
        }

        public Task RevokeFamilyAsync(
            Guid familyId,
            DateTime revokedAt,
            CancellationToken cancellationToken = default)
        {
            RevokedFamilyId = familyId;
            return Task.CompletedTask;
        }
    }

    private sealed class StubRefreshTokenGenerator : IRefreshTokenGenerator
    {
        public IssuedRefreshToken Generate(int userId, Guid? familyId = null) =>
            new(
                "new-refresh-token",
                RefreshTokenSession.Create(
                    userId,
                    familyId ?? Guid.NewGuid(),
                    "new-token-hash",
                    Now,
                    Now.AddDays(7)));

        public string Hash(string token) => token switch
        {
            "old-token" => "old-token-hash",
            _ => $"{token}-hash"
        };
    }

    private sealed class StubAccessTokenGenerator : IAccessTokenGenerator
    {
        public AccessToken Generate(UserAccount account) =>
            new("new-access-token", Now.AddMinutes(15));
    }

    private sealed class FixedClock(DateTime utcNow) : IClock
    {
        public DateTime UtcNow { get; } = utcNow;
    }
}
