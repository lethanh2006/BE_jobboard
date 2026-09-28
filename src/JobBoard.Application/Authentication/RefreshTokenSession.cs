namespace JobBoard.Application.Authentication;

public sealed class RefreshTokenSession
{
    private RefreshTokenSession(
        Guid id,
        int userId,
        Guid familyId,
        string tokenHash,
        DateTime createdAt,
        DateTime expiresAt,
        DateTime? revokedAt,
        string? replacedByTokenHash)
    {
        if (id == Guid.Empty || familyId == Guid.Empty)
        {
            throw new ArgumentException("Mã phiên đăng nhập không hợp lệ.");
        }

        if (userId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(userId));
        }

        if (string.IsNullOrWhiteSpace(tokenHash))
        {
            throw new ArgumentException("Hash refresh token không được để trống.", nameof(tokenHash));
        }

        if (expiresAt <= createdAt)
        {
            throw new ArgumentException("Thời hạn refresh token không hợp lệ.", nameof(expiresAt));
        }

        Id = id;
        UserId = userId;
        FamilyId = familyId;
        TokenHash = tokenHash;
        CreatedAt = createdAt;
        ExpiresAt = expiresAt;
        RevokedAt = revokedAt;
        ReplacedByTokenHash = replacedByTokenHash;
    }

    public Guid Id { get; }

    public int UserId { get; }

    public Guid FamilyId { get; }

    public string TokenHash { get; }

    public DateTime CreatedAt { get; }

    public DateTime ExpiresAt { get; }

    public DateTime? RevokedAt { get; private set; }

    public string? ReplacedByTokenHash { get; private set; }

    public bool IsActiveAt(DateTime utcNow) => RevokedAt is null && ExpiresAt > utcNow;

    public static RefreshTokenSession Create(
        int userId,
        Guid familyId,
        string tokenHash,
        DateTime createdAt,
        DateTime expiresAt) =>
        new(Guid.NewGuid(), userId, familyId, tokenHash, createdAt, expiresAt, null, null);

    public static RefreshTokenSession Restore(
        Guid id,
        int userId,
        Guid familyId,
        string tokenHash,
        DateTime createdAt,
        DateTime expiresAt,
        DateTime? revokedAt,
        string? replacedByTokenHash) =>
        new(
            id,
            userId,
            familyId,
            tokenHash,
            createdAt,
            expiresAt,
            revokedAt,
            replacedByTokenHash);

    public void Revoke(DateTime revokedAt, string? replacedByTokenHash = null)
    {
        if (RevokedAt is not null)
        {
            return;
        }

        RevokedAt = revokedAt;
        ReplacedByTokenHash = replacedByTokenHash;
    }
}

public sealed record IssuedRefreshToken(string Value, RefreshTokenSession Session);
