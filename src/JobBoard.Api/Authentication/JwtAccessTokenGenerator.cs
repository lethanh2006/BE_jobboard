using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using JobBoard.Application.Abstractions.Authentication;
using JobBoard.Application.Authentication;
using JobBoard.Domain.Abstractions;
using Microsoft.IdentityModel.Tokens;

namespace JobBoard.Api.Authentication;

public sealed class JwtAccessTokenGenerator(JwtOptions options, IClock clock) : IAccessTokenGenerator
{
    private readonly IClock _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    private readonly JwtOptions _options = options ?? throw new ArgumentNullException(nameof(options));

    public AccessToken Generate(UserAccount account)
    {
        ArgumentNullException.ThrowIfNull(account);

        var issuedAt = _clock.UtcNow;
        var expiresAt = issuedAt.AddMinutes(_options.AccessTokenMinutes);
        var claims = new List<Claim>
        {
            new(JobBoardClaimTypes.Subject, account.Id.ToString(CultureInfo.InvariantCulture)),
            new(JobBoardClaimTypes.Name, account.Name),
            new(JobBoardClaimTypes.Email, account.Email),
            new(JobBoardClaimTypes.Role, account.Role.ToString())
        };

        if (account.CompanyId is int companyId)
        {
            claims.Add(new Claim(
                JobBoardClaimTypes.CompanyId,
                companyId.ToString(CultureInfo.InvariantCulture)));
        }

        if (!string.IsNullOrWhiteSpace(account.CompanyName))
        {
            claims.Add(new Claim(JobBoardClaimTypes.CompanyName, account.CompanyName));
        }

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: issuedAt,
            expires: expiresAt,
            signingCredentials: new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256));

        return new AccessToken(new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
