using JobBoard.Application.Abstractions.Authentication;
using JobBoard.Application.Authentication;
using JobBoard.Application.Exceptions;
using JobBoard.Domain.Entities;

namespace JobBoard.Application.Tests.Authentication;

public sealed class RegistrationServiceTests
{
    private static readonly DateTime Now = new(2026, 9, 28, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task RegisterCandidateAsync_WithValidData_CreatesProfileAndSession()
    {
        var registrations = new StubRegistrationRepository();
        var refreshTokens = new StubRefreshTokenRepository();
        var service = CreateService(registrations, refreshTokens);

        var result = await service.RegisterCandidateAsync(
            "  Trần Thu Hà  ",
            "  HA@EXAMPLE.COM ",
            "StrongPass@123");

        Assert.Equal(101, result.User.Id);
        Assert.Equal("Trần Thu Hà", result.User.Name);
        Assert.Equal("ha@example.com", result.User.Email);
        Assert.Equal(AccountRole.Candidate, result.User.Role);
        Assert.Equal(result.User.Id, registrations.Candidate?.Id);
        Assert.Equal("hashed:StrongPass@123", registrations.Account?.PasswordHash);
        Assert.NotNull(refreshTokens.AddedSession);
    }

    [Theory]
    [InlineData("short")]
    [InlineData("alllowercase123!")]
    [InlineData("ALLUPPERCASE123!")]
    [InlineData("NoNumberHere!")]
    [InlineData("NoSpecial123")]
    public async Task RegisterCandidateAsync_WithWeakPassword_Throws(string password)
    {
        var service = CreateService(
            new StubRegistrationRepository(),
            new StubRefreshTokenRepository());

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.RegisterCandidateAsync("Trần Thu Hà", "ha@example.com", password));
    }

    [Fact]
    public async Task RegisterCandidateAsync_WithExistingEmail_ThrowsConflict()
    {
        var service = CreateService(
            new StubRegistrationRepository(emailExists: true),
            new StubRefreshTokenRepository());

        await Assert.ThrowsAsync<EmailAlreadyExistsException>(() =>
            service.RegisterCandidateAsync(
                "Trần Thu Hà",
                "ha@example.com",
                "StrongPass@123"));
    }

    private static RegistrationService CreateService(
        StubRegistrationRepository registrations,
        StubRefreshTokenRepository refreshTokens) =>
        new(
            registrations,
            new StubPasswordHasher(),
            new StubAccessTokenGenerator(),
            new StubRefreshTokenGenerator(),
            refreshTokens);

    private sealed class StubRegistrationRepository(bool emailExists = false)
        : IAccountRegistrationRepository
    {
        public UserAccount? Account { get; private set; }

        public Candidate? Candidate { get; private set; }

        public Task<int> GetNextUserIdAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(101);

        public Task<bool> EmailExistsAsync(
            string normalizedEmail,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(emailExists);

        public Task<bool> RegisterCandidateAsync(
            UserAccount account,
            Candidate candidate,
            CancellationToken cancellationToken = default)
        {
            Account = account;
            Candidate = candidate;
            return Task.FromResult(true);
        }
    }

    private sealed class StubPasswordHasher : IPasswordHasher
    {
        public string Hash(string password) => $"hashed:{password}";

        public bool Verify(string password, string passwordHash) => false;
    }

    private sealed class StubAccessTokenGenerator : IAccessTokenGenerator
    {
        public AccessToken Generate(UserAccount account) =>
            new("access-token", Now.AddMinutes(15));
    }

    private sealed class StubRefreshTokenGenerator : IRefreshTokenGenerator
    {
        public IssuedRefreshToken Generate(int userId, Guid? familyId = null) =>
            new(
                "refresh-token",
                RefreshTokenSession.Create(
                    userId,
                    familyId ?? Guid.NewGuid(),
                    "refresh-token-hash",
                    Now,
                    Now.AddDays(7)));

        public string Hash(string token) => "refresh-token-hash";
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

        public Task RevokeByUserAsync(
            int userId,
            DateTime revokedAt,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
