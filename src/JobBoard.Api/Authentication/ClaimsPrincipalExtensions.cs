using System.Globalization;
using System.Security.Claims;

namespace JobBoard.Api.Authentication;

public static class ClaimsPrincipalExtensions
{
    public static int GetRequiredUserId(this ClaimsPrincipal principal) =>
        GetRequiredIntClaim(principal, JobBoardClaimTypes.Subject);

    public static int GetRequiredCompanyId(this ClaimsPrincipal principal) =>
        GetRequiredIntClaim(principal, JobBoardClaimTypes.CompanyId);

    public static string GetRequiredClaim(this ClaimsPrincipal principal, string claimType) =>
        principal.FindFirstValue(claimType)
        ?? throw new InvalidOperationException($"Token thiếu claim bắt buộc '{claimType}'.");

    private static int GetRequiredIntClaim(ClaimsPrincipal principal, string claimType)
    {
        var value = principal.GetRequiredClaim(claimType);
        return int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var result)
            ? result
            : throw new InvalidOperationException($"Claim '{claimType}' không hợp lệ.");
    }
}
