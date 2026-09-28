using JobBoard.Application.Abstractions.Authentication;
using JobBoard.Application.Exceptions;
using JobBoard.Domain.Abstractions;

namespace JobBoard.Application.Authentication;

public sealed class SessionService(
    IUserAccountRepository accounts,
    IRefreshTokenRepository refreshTokens,
    IRefreshTokenGenerator refreshTokenGenerator,
    IAccessTokenGenerator accessTokenGenerator,
    IClock clock)
{
    private const string InvalidTokenMessage = "Phiên đăng nhập không hợp lệ hoặc đã hết hạn.";

    private readonly IAccessTokenGenerator _accessTokenGenerator =
        accessTokenGenerator ?? throw new ArgumentNullException(nameof(accessTokenGenerator));
    private readonly IUserAccountRepository _accounts =
        accounts ?? throw new ArgumentNullException(nameof(accounts));
    private readonly IClock _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    private readonly IRefreshTokenGenerator _refreshTokenGenerator =
        refreshTokenGenerator ?? throw new ArgumentNullException(nameof(refreshTokenGenerator));
    private readonly IRefreshTokenRepository _refreshTokens =
        refreshTokens ?? throw new ArgumentNullException(nameof(refreshTokens));

    public async Task<LoginResult> RefreshAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            throw new InvalidRefreshTokenException(InvalidTokenMessage);
        }

        var tokenHash = _refreshTokenGenerator.Hash(refreshToken);
        var currentSession = await _refreshTokens.FindByHashAsync(tokenHash, cancellationToken)
            ?? throw new InvalidRefreshTokenException(InvalidTokenMessage);
        var now = _clock.UtcNow;

        if (currentSession.RevokedAt is not null)
        {
            await _refreshTokens.RevokeFamilyAsync(
                currentSession.FamilyId,
                now,
                cancellationToken);
            throw new InvalidRefreshTokenException(InvalidTokenMessage);
        }

        if (!currentSession.IsActiveAt(now))
        {
            await _refreshTokens.RevokeByHashAsync(tokenHash, now, cancellationToken);
            throw new InvalidRefreshTokenException(InvalidTokenMessage);
        }

        var account = await _accounts.FindByIdAsync(currentSession.UserId, cancellationToken)
            ?? throw new InvalidRefreshTokenException(InvalidTokenMessage);
        var replacement = _refreshTokenGenerator.Generate(account.Id, currentSession.FamilyId);
        var rotated = await _refreshTokens.TryRotateAsync(
            currentSession.Id,
            replacement.Session.TokenHash,
            replacement.Session,
            now,
            cancellationToken);

        if (!rotated)
        {
            await _refreshTokens.RevokeFamilyAsync(currentSession.FamilyId, now, cancellationToken);
            throw new InvalidRefreshTokenException(InvalidTokenMessage);
        }

        var accessToken = _accessTokenGenerator.Generate(account);
        return new LoginResult(
            accessToken.Value,
            accessToken.ExpiresAt,
            replacement.Value,
            replacement.Session.ExpiresAt,
            MapUser(account));
    }

    public async Task LogoutAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return;
        }

        await _refreshTokens.RevokeByHashAsync(
            _refreshTokenGenerator.Hash(refreshToken),
            _clock.UtcNow,
            cancellationToken);
    }

    private static AuthenticatedUserDto MapUser(UserAccount account) =>
        new(
            account.Id,
            account.Name,
            account.Email,
            account.Role,
            account.CompanyId,
            account.CompanyName);
}
