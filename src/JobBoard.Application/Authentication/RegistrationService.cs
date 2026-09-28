using System.ComponentModel.DataAnnotations;
using JobBoard.Application.Abstractions.Authentication;
using JobBoard.Application.Exceptions;
using JobBoard.Domain.Entities;

namespace JobBoard.Application.Authentication;

public sealed class RegistrationService(
    IAccountRegistrationRepository registrations,
    IPasswordHasher passwordHasher,
    IAccessTokenGenerator accessTokenGenerator,
    IRefreshTokenGenerator refreshTokenGenerator,
    IRefreshTokenRepository refreshTokens)
{
    private readonly IAccessTokenGenerator _accessTokenGenerator =
        accessTokenGenerator ?? throw new ArgumentNullException(nameof(accessTokenGenerator));
    private readonly IPasswordHasher _passwordHasher =
        passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
    private readonly IRefreshTokenGenerator _refreshTokenGenerator =
        refreshTokenGenerator ?? throw new ArgumentNullException(nameof(refreshTokenGenerator));
    private readonly IRefreshTokenRepository _refreshTokens =
        refreshTokens ?? throw new ArgumentNullException(nameof(refreshTokens));
    private readonly IAccountRegistrationRepository _registrations =
        registrations ?? throw new ArgumentNullException(nameof(registrations));

    public async Task<LoginResult> RegisterCandidateAsync(
        string name,
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        PasswordPolicy.Validate(password);
        var normalizedName = name?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            throw new ArgumentException("Tên người dùng không được để trống.", nameof(name));
        }

        var normalizedEmail = NormalizeEmail(email);

        if (await _registrations.EmailExistsAsync(normalizedEmail, cancellationToken))
        {
            throw new EmailAlreadyExistsException("Email này đã được sử dụng.");
        }

        var userId = await _registrations.GetNextUserIdAsync(cancellationToken);
        var passwordHash = _passwordHasher.Hash(password);
        var account = new UserAccount(
            userId,
            normalizedName,
            normalizedEmail,
            passwordHash,
            AccountRole.Candidate);
        var candidate = new Candidate(userId, normalizedName, normalizedEmail);

        if (!await _registrations.RegisterCandidateAsync(account, candidate, cancellationToken))
        {
            throw new EmailAlreadyExistsException("Email này đã được sử dụng.");
        }

        var accessToken = _accessTokenGenerator.Generate(account);
        var refreshToken = _refreshTokenGenerator.Generate(account.Id);
        await _refreshTokens.AddAsync(refreshToken.Session, cancellationToken);

        return new LoginResult(
            accessToken.Value,
            accessToken.ExpiresAt,
            refreshToken.Value,
            refreshToken.Session.ExpiresAt,
            new AuthenticatedUserDto(
                account.Id,
                account.Name,
                account.Email,
                account.Role,
                account.CompanyId,
                account.CompanyName));
    }

    private static string NormalizeEmail(string email)
    {
        var normalizedEmail = email?.Trim().ToLowerInvariant() ?? string.Empty;
        if (!new EmailAddressAttribute().IsValid(normalizedEmail))
        {
            throw new ArgumentException("Email không hợp lệ.", nameof(email));
        }

        return normalizedEmail;
    }

}
