using JobBoard.Application.Abstractions.Authentication;
using JobBoard.Application.Authentication;
using JobBoard.Application.Exceptions;

namespace JobBoard.Application.Tests.Authentication;

public sealed class LoginServiceTests
{
    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsTokenAndUser()
    {
        var account = new UserAccount(
            20,
            "Nguyễn Minh An",
            "candidate@jobboard.vn",
            "stored-hash",
            AccountRole.Candidate);
        var repository = new StubAccountRepository(account);
        var refreshTokens = new StubRefreshTokenRepository();
        var service = new LoginService(
            repository,
            new StubPasswordHasher(isValid: true),
            new StubTokenGenerator(),
            new StubRefreshTokenGenerator(),
            refreshTokens);

        var result = await service.LoginAsync("  CANDIDATE@JOBBOARD.VN ", "JobBoard@123");

        Assert.Equal("demo-token", result.AccessToken);
        Assert.Equal("demo-refresh-token", result.RefreshToken);
        Assert.Equal(account.Id, result.User.Id);
        Assert.Equal(AccountRole.Candidate, result.User.Role);
        Assert.Equal("candidate@jobboard.vn", repository.LastEmail);
        Assert.NotNull(refreshTokens.AddedSession);
    }

    [Fact]
    public async Task LoginAsync_WithUnknownEmail_ThrowsSameAuthenticationError()
    {
        var service = new LoginService(
            new StubAccountRepository(),
            new StubPasswordHasher(isValid: true),
            new StubTokenGenerator(),
            new StubRefreshTokenGenerator(),
            new StubRefreshTokenRepository());

        var exception = await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => service.LoginAsync("unknown@jobboard.vn", "JobBoard@123"));

        Assert.Equal("Email hoặc mật khẩu không chính xác.", exception.Message);
    }

    [Fact]
    public async Task LoginAsync_WithWrongPassword_ThrowsSameAuthenticationError()
    {
        var account = new UserAccount(
            1,
            "Trần Bình",
            "employer@jobboard.vn",
            "stored-hash",
            AccountRole.Employer,
            CompanyId: 1);
        var service = new LoginService(
            new StubAccountRepository(account),
            new StubPasswordHasher(isValid: false),
            new StubTokenGenerator(),
            new StubRefreshTokenGenerator(),
            new StubRefreshTokenRepository());

        var exception = await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => service.LoginAsync(account.Email, "wrong-password"));

        Assert.Equal("Email hoặc mật khẩu không chính xác.", exception.Message);
    }

    private sealed class StubAccountRepository(params UserAccount[] accounts) : IUserAccountRepository
    {
        public string? LastEmail { get; private set; }

        public Task<UserAccount?> FindByEmailAsync(
            string normalizedEmail,
            CancellationToken cancellationToken = default)
        {
            LastEmail = normalizedEmail;
            return Task.FromResult(accounts.FirstOrDefault(
                account => account.Email.Equals(normalizedEmail, StringComparison.OrdinalIgnoreCase)));
        }

        public Task<UserAccount?> FindByIdAsync(
            int id,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(accounts.FirstOrDefault(account => account.Id == id));
    }

    private sealed class StubPasswordHasher(bool isValid) : IPasswordHasher
    {
        public string Hash(string password) => "stored-hash";

        public bool Verify(string password, string passwordHash) => isValid;
    }

    private sealed class StubTokenGenerator : IAccessTokenGenerator
    {
        public AccessToken Generate(UserAccount account) =>
            new("demo-token", new DateTime(2026, 9, 28, 12, 0, 0, DateTimeKind.Utc));
    }

    private sealed class StubRefreshTokenGenerator : IRefreshTokenGenerator
    {
        public IssuedRefreshToken Generate(int userId, Guid? familyId = null)
        {
            var createdAt = new DateTime(2026, 9, 28, 12, 0, 0, DateTimeKind.Utc);
            return new IssuedRefreshToken(
                "demo-refresh-token",
                RefreshTokenSession.Create(
                    userId,
                    familyId ?? Guid.NewGuid(),
                    "demo-refresh-hash",
                    createdAt,
                    createdAt.AddDays(7)));
        }

        public string Hash(string token) => "demo-refresh-hash";
    }

    private sealed class StubRefreshTokenRepository : IRefreshTokenRepository
    {
        public RefreshTokenSession? AddedSession { get; private set; }

        public Task<RefreshTokenSession?> FindByHashAsync(
            string tokenHash,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<RefreshTokenSession?>(null);

        public Task AddAsync(
            RefreshTokenSession session,
            CancellationToken cancellationToken = default)
        {
            AddedSession = session;
            return Task.CompletedTask;
        }

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
    }
}
