using JobBoard.Application.Abstractions.Authentication;
using JobBoard.Application.Authentication;
using JobBoard.Application.Exceptions;
using JobBoard.Domain.Abstractions;

namespace JobBoard.Application.Tests.Authentication;

public sealed class PasswordChangeServiceTests
{
    private static readonly DateTime Now = new(2026, 9, 28, 12, 0, 0, DateTimeKind.Utc);
    private static readonly UserAccount Account = new(
        20,
        "Nguyễn Minh An",
        "candidate@jobboard.vn",
        "hash:CurrentPass@123",
        AccountRole.Candidate);

    [Fact]
    public async Task ChangeAsync_WithValidCredentials_UpdatesHashAndRevokesEverySession()
    {
        var security = new StubAccountSecurityRepository();
        var refreshTokens = new StubRefreshTokenRepository();
        var service = CreateService(security, refreshTokens);

        await service.ChangeAsync(Account.Id, "CurrentPass@123", "NewSecurePass@456");

        Assert.Equal(Account.Id, security.UserId);
        Assert.Equal(Account.PasswordHash, security.ExpectedPasswordHash);
        Assert.Equal("hash:NewSecurePass@456", security.NewPasswordHash);
        Assert.Equal(Account.Id, refreshTokens.RevokedUserId);
        Assert.Equal(Now, refreshTokens.RevokedAt);
    }

    [Fact]
    public async Task ChangeAsync_WithWrongCurrentPassword_DoesNotUpdatePassword()
    {
        var security = new StubAccountSecurityRepository();
        var refreshTokens = new StubRefreshTokenRepository();
        var service = CreateService(security, refreshTokens);

        await Assert.ThrowsAsync<CurrentPasswordIncorrectException>(() =>
            service.ChangeAsync(Account.Id, "WrongPassword@123", "NewSecurePass@456"));

        Assert.Null(security.UserId);
        Assert.Null(refreshTokens.RevokedUserId);
    }

    [Fact]
    public async Task ChangeAsync_WithSamePassword_RejectsRequest()
    {
        var service = CreateService(
            new StubAccountSecurityRepository(),
            new StubRefreshTokenRepository());

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.ChangeAsync(Account.Id, "CurrentPass@123", "CurrentPass@123"));

        Assert.Contains("phải khác", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ChangeAsync_WithWeakNewPassword_RejectsRequest()
    {
        var service = CreateService(
            new StubAccountSecurityRepository(),
            new StubRefreshTokenRepository());

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.ChangeAsync(Account.Id, "CurrentPass@123", "weak"));
    }

    [Fact]
    public async Task ChangeAsync_WhenPasswordChangedConcurrently_RequiresLoginAgain()
    {
        var service = CreateService(
            new StubAccountSecurityRepository(updateSucceeds: false),
            new StubRefreshTokenRepository());

        var exception = await Assert.ThrowsAsync<CurrentPasswordIncorrectException>(() =>
            service.ChangeAsync(Account.Id, "CurrentPass@123", "NewSecurePass@456"));

        Assert.Contains("yêu cầu khác", exception.Message, StringComparison.Ordinal);
    }

    private static PasswordChangeService CreateService(
        StubAccountSecurityRepository security,
        StubRefreshTokenRepository refreshTokens) =>
        new(
            new StubAccountRepository(),
            security,
            new StubPasswordHasher(),
            refreshTokens,
            new FixedClock(Now));

    private sealed class StubAccountRepository : IUserAccountRepository
    {
        public Task<UserAccount?> FindByIdAsync(
            int id,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<UserAccount?>(id == Account.Id ? Account : null);

        public Task<UserAccount?> FindByEmailAsync(
            string normalizedEmail,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<UserAccount?>(null);
    }

    private sealed class StubAccountSecurityRepository(bool updateSucceeds = true)
        : IAccountSecurityRepository
    {
        public int? UserId { get; private set; }

        public string? ExpectedPasswordHash { get; private set; }

        public string? NewPasswordHash { get; private set; }

        public Task<bool> TryUpdatePasswordHashAsync(
            int userId,
            string expectedPasswordHash,
            string newPasswordHash,
            CancellationToken cancellationToken = default)
        {
            UserId = userId;
            ExpectedPasswordHash = expectedPasswordHash;
            NewPasswordHash = newPasswordHash;
            return Task.FromResult(updateSucceeds);
        }
    }

    private sealed class StubPasswordHasher : IPasswordHasher
    {
        public string Hash(string password) => $"hash:{password}";

        public bool Verify(string password, string passwordHash) =>
            passwordHash == Hash(password);
    }

    private sealed class StubRefreshTokenRepository : IRefreshTokenRepository
    {
        public int? RevokedUserId { get; private set; }

        public DateTime? RevokedAt { get; private set; }

        public Task<RefreshTokenSession?> FindByHashAsync(
            string tokenHash,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<RefreshTokenSession?>(null);

        public Task AddAsync(
            RefreshTokenSession session,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<bool> TryRotateAsync(
            Guid currentSessionId,
            string replacementTokenHash,
            RefreshTokenSession replacement,
            DateTime revokedAt,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task RevokeByHashAsync(
            string tokenHash,
            DateTime revokedAt,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task RevokeFamilyAsync(
            Guid familyId,
            DateTime revokedAt,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task RevokeByUserAsync(
            int userId,
            DateTime revokedAt,
            CancellationToken cancellationToken = default)
        {
            RevokedUserId = userId;
            RevokedAt = revokedAt;
            return Task.CompletedTask;
        }
    }

    private sealed class FixedClock(DateTime utcNow) : IClock
    {
        public DateTime UtcNow { get; } = utcNow;
    }
}
