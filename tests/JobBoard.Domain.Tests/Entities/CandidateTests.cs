using JobBoard.Domain.Entities;

namespace JobBoard.Domain.Tests.Entities;

public sealed class CandidateTests
{
    [Fact]
    public void Constructor_WithValidData_CreatesCandidate()
    {
        var candidate = new Candidate(1, "  Nguyễn An  ", "  an@example.com  ", "  Backend developer  ");

        Assert.Equal(1, candidate.Id);
        Assert.Equal("Nguyễn An", candidate.Name);
        Assert.Equal("an@example.com", candidate.Email);
        Assert.Equal("Backend developer", candidate.Bio);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithEmptyName_Throws(string? name)
    {
        var exception = Assert.Throws<ArgumentException>(() => new Candidate(1, name!, "an@example.com"));

        Assert.Equal("name", exception.ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithEmptyEmail_Throws(string? email)
    {
        var exception = Assert.Throws<ArgumentException>(() => new Candidate(1, "Nguyễn An", email!));

        Assert.Equal("email", exception.ParamName);
    }
}
