using JobBoard.Application.Abstractions.Authentication;
using JobBoard.Application.Exceptions;
using JobBoard.Domain.Abstractions;

namespace JobBoard.Application.Authentication;

public sealed class PasswordChangeService(
    IUserAccountRepository accounts,
    IAccountSecurityRepository accountSecurity,
    IPasswordHasher passwordHasher,
    IRefreshTokenRepository refreshTokens,
    IClock clock)
{
    private readonly IAccountSecurityRepository _accountSecurity =
        accountSecurity ?? throw new ArgumentNullException(nameof(accountSecurity));
    private readonly IUserAccountRepository _accounts =
        accounts ?? throw new ArgumentNullException(nameof(accounts));
    private readonly IClock _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    private readonly IPasswordHasher _passwordHasher =
        passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
    private readonly IRefreshTokenRepository _refreshTokens =
        refreshTokens ?? throw new ArgumentNullException(nameof(refreshTokens));

    public async Task ChangeAsync(
        int userId,
        string currentPassword,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(currentPassword))
        {
            throw new CurrentPasswordIncorrectException("Mật khẩu hiện tại không chính xác.");
        }

        PasswordPolicy.Validate(newPassword);
        var account = await _accounts.FindByIdAsync(userId, cancellationToken)
            ?? throw new ResourceNotFoundException("Không tìm thấy tài khoản.");

        if (!_passwordHasher.Verify(currentPassword, account.PasswordHash))
        {
            throw new CurrentPasswordIncorrectException("Mật khẩu hiện tại không chính xác.");
        }

        if (_passwordHasher.Verify(newPassword, account.PasswordHash))
        {
            throw new ArgumentException(
                "Mật khẩu mới phải khác mật khẩu hiện tại.",
                nameof(newPassword));
        }

        var newPasswordHash = _passwordHasher.Hash(newPassword);
        var updated = await _accountSecurity.TryUpdatePasswordHashAsync(
            userId,
            account.PasswordHash,
            newPasswordHash,
            cancellationToken);

        if (!updated)
        {
            throw new CurrentPasswordIncorrectException(
                "Mật khẩu đã được thay đổi bởi một yêu cầu khác. Vui lòng đăng nhập lại.");
        }

        await _refreshTokens.RevokeByUserAsync(userId, _clock.UtcNow, cancellationToken);
    }
}
