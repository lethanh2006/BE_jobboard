using JobBoard.Application.Authentication;
using JobBoard.Domain.Abstractions;
using JobBoard.Infrastructure.Authentication;

namespace JobBoard.Infrastructure.Tests.Authentication;

public sealed class RefreshTokenTests
{
    private static readonly DateTime Now = new(2026, 9, 28, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Generate_CreatesRandomTokenAndOnlyStoresItsHash()
    {
        var generator = new SecureRefreshTokenGenerator(new FixedClock(Now), TimeSpan.FromDays(7));

        var first = generator.Generate(20);
        var second = generator.Generate(20);

        Assert.NotEqual(first.Value, second.Value);
        Assert.Equal(generator.Hash(first.Value), first.Session.TokenHash);
        Assert.DoesNotContain(first.Value, first.Session.TokenHash, StringComparison.Ordinal);
        Assert.Equal(Now.AddDays(7), first.Session.ExpiresAt);
    }

    [Fact]
    public async Task TryRotateAsync_AllowsCurrentTokenOnlyOnce()
    {
        var repository = new InMemoryRefreshTokenRepository();
        var current = RefreshTokenSession.Create(
            20,
            Guid.NewGuid(),
            "current-hash",
            Now,
            Now.AddDays(7));
        var firstReplacement = RefreshTokenSession.Create(
            20,
            current.FamilyId,
            "first-replacement-hash",
            Now.AddMinutes(1),
            Now.AddDays(7));
        var secondReplacement = RefreshTokenSession.Create(
            20,
            current.FamilyId,
            "second-replacement-hash",
            Now.AddMinutes(1),
            Now.AddDays(7));
        await repository.AddAsync(current);

        var firstResult = await repository.TryRotateAsync(
            current.Id,
            firstReplacement.TokenHash,
            firstReplacement,
            Now.AddMinutes(1));
        var secondResult = await repository.TryRotateAsync(
            current.Id,
            secondReplacement.TokenHash,
            secondReplacement,
            Now.AddMinutes(1));

        Assert.True(firstResult);
        Assert.False(secondResult);
        Assert.NotNull((await repository.FindByHashAsync(current.TokenHash))?.RevokedAt);
        Assert.NotNull(await repository.FindByHashAsync(firstReplacement.TokenHash));
        Assert.Null(await repository.FindByHashAsync(secondReplacement.TokenHash));
    }

    private sealed class FixedClock(DateTime utcNow) : IClock
    {
        public DateTime UtcNow { get; } = utcNow;
    }
}
