using System.Security.Cryptography;
using System.Text;
using JobBoard.Application.Abstractions.Authentication;
using JobBoard.Application.Authentication;
using JobBoard.Domain.Abstractions;

namespace JobBoard.Infrastructure.Authentication;

public sealed class SecureRefreshTokenGenerator(
    IClock clock,
    TimeSpan lifetime) : IRefreshTokenGenerator
{
    private const int TokenBytes = 64;
    private readonly IClock _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    private readonly TimeSpan _lifetime = lifetime > TimeSpan.Zero
        ? lifetime
        : throw new ArgumentOutOfRangeException(nameof(lifetime));

    public IssuedRefreshToken Generate(int userId, Guid? familyId = null)
    {
        var token = ToBase64Url(RandomNumberGenerator.GetBytes(TokenBytes));
        var createdAt = _clock.UtcNow;
        var session = RefreshTokenSession.Create(
            userId,
            familyId ?? Guid.NewGuid(),
            Hash(token),
            createdAt,
            createdAt.Add(_lifetime));

        return new IssuedRefreshToken(token, session);
    }

    public string Hash(string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }

    private static string ToBase64Url(byte[] value) =>
        Convert.ToBase64String(value)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
}
