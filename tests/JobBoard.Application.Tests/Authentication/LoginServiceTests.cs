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
        var service = new LoginService(
            repository,
            new StubPasswordHasher(isValid: true),
            new StubTokenGenerator());

        var result = await service.LoginAsync("  CANDIDATE@JOBBOARD.VN ", "JobBoard@123");

        Assert.Equal("demo-token", result.AccessToken);
        Assert.Equal(account.Id, result.User.Id);
        Assert.Equal(AccountRole.Candidate, result.User.Role);
        Assert.Equal("candidate@jobboard.vn", repository.LastEmail);
    }

    [Fact]
    public async Task LoginAsync_WithUnknownEmail_ThrowsSameAuthenticationError()
    {
        var service = new LoginService(
            new StubAccountRepository(),
            new StubPasswordHasher(isValid: true),
            new StubTokenGenerator());

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
            new StubTokenGenerator());

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
}
