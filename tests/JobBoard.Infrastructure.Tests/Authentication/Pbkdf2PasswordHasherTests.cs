using JobBoard.Infrastructure.Authentication;

namespace JobBoard.Infrastructure.Tests.Authentication;

public sealed class Pbkdf2PasswordHasherTests
{
    [Fact]
    public void HashAndVerify_WithMatchingPassword_ReturnsTrue()
    {
        var hasher = new Pbkdf2PasswordHasher();
        var hash = hasher.Hash("JobBoard@123");

        Assert.True(hasher.Verify("JobBoard@123", hash));
        Assert.False(hasher.Verify("wrong-password", hash));
    }

    [Theory]
    [InlineData("")]
    [InlineData("invalid-hash")]
    [InlineData("v1.not-a-number.salt.hash")]
    [InlineData("v1.1000.not-base64.not-base64")]
    public void Verify_WithMalformedHash_ReturnsFalse(string hash)
    {
        var hasher = new Pbkdf2PasswordHasher();

        Assert.False(hasher.Verify("JobBoard@123", hash));
    }
}
