namespace JobBoard.Application.Authentication;

public sealed record AccessToken(string Value, DateTime ExpiresAt);

public sealed record AuthenticatedUserDto(
    int Id,
    string Name,
    string Email,
    AccountRole Role,
    int? CompanyId,
    string? CompanyName);

public sealed record LoginResult(
    string AccessToken,
    DateTime ExpiresAt,
    string RefreshToken,
    DateTime RefreshExpiresAt,
    AuthenticatedUserDto User);
