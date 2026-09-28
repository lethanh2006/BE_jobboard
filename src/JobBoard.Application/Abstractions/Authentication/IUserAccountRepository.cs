using JobBoard.Application.Authentication;

namespace JobBoard.Application.Abstractions.Authentication;

public interface IUserAccountRepository
{
    Task<UserAccount?> FindByEmailAsync(
        string normalizedEmail,
        CancellationToken cancellationToken = default);
}
