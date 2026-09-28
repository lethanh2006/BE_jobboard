namespace JobBoard.Application.Authentication;

public sealed record UserAccount(
    int Id,
    string Name,
    string Email,
    string PasswordHash,
    AccountRole Role,
    int? CompanyId = null,
    string? CompanyName = null);
