using JobBoard.Application.Abstractions.Authentication;
using JobBoard.Application.Exceptions;

namespace JobBoard.Application.Authentication;

public sealed class LoginService(
    IUserAccountRepository accounts,
    IPasswordHasher passwordHasher,
    IAccessTokenGenerator accessTokenGenerator)
{
    private readonly IAccessTokenGenerator _accessTokenGenerator =
        accessTokenGenerator ?? throw new ArgumentNullException(nameof(accessTokenGenerator));
    private readonly IUserAccountRepository _accounts =
        accounts ?? throw new ArgumentNullException(nameof(accounts));
    private readonly IPasswordHasher _passwordHasher =
        passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));

    public async Task<LoginResult> LoginAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidCredentialsException("Email hoặc mật khẩu không chính xác.");
        }

        var normalizedEmail = email.Trim().ToLowerInvariant();
        var account = await _accounts.FindByEmailAsync(normalizedEmail, cancellationToken);

        if (account is null || !_passwordHasher.Verify(password, account.PasswordHash))
        {
            throw new InvalidCredentialsException("Email hoặc mật khẩu không chính xác.");
        }

        var token = _accessTokenGenerator.Generate(account);

        return new LoginResult(
            token.Value,
            token.ExpiresAt,
            new AuthenticatedUserDto(
                account.Id,
                account.Name,
                account.Email,
                account.Role,
                account.CompanyId,
                account.CompanyName));
    }
}
