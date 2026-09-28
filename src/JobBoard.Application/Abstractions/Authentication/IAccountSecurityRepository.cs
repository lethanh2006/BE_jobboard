namespace JobBoard.Application.Abstractions.Authentication;

public interface IAccountSecurityRepository
{
    Task<bool> TryUpdatePasswordHashAsync(
        int userId,
        string expectedPasswordHash,
        string newPasswordHash,
        CancellationToken cancellationToken = default);
}
