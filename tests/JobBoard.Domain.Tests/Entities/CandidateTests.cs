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

    [Fact]
    public void Constructor_WithEmailMissingAtSign_Throws()
    {
        var exception = Assert.Throws<ArgumentException>(() => new Candidate(1, "Nguyễn An", "invalid-email"));

        Assert.Equal("email", exception.ParamName);
    }

    [Fact]
    public void ChangeEmail_WithValidEmail_UpdatesEmail()
    {
        var candidate = new Candidate(1, "Nguyễn An", "old@example.com");

        candidate.ChangeEmail("  new@example.com  ");

        Assert.Equal("new@example.com", candidate.Email);
    }

    [Fact]
    public void ChangeEmail_WithInvalidEmail_PreservesCurrentEmail()
    {
        var candidate = new Candidate(1, "Nguyễn An", "old@example.com");

        Assert.Throws<ArgumentException>(() => candidate.ChangeEmail("invalid-email"));
        Assert.Equal("old@example.com", candidate.Email);
    }

    [Fact]
    public void GetDisplayName_ReturnsCandidateName()
    {
        var candidate = new Candidate(1, "Nguyễn An", "an@example.com");

        Assert.Equal("Nguyễn An", candidate.GetDisplayName());
    }

    [Fact]
    public void AddSkill_WithSameNameIgnoringCase_DoesNotAddDuplicate()
    {
        var candidate = new Candidate(1, "Nguyễn An", "an@example.com");

        var firstAdded = candidate.AddSkill(new Skill("C#"));
        var duplicateAdded = candidate.AddSkill(new Skill(" c# "));

        Assert.True(firstAdded);
        Assert.False(duplicateAdded);
        Assert.Single(candidate.Skills);
    }

    [Fact]
    public void UpdateProfile_ReplacesContactSalaryAndSkills()
    {
        var candidate = new Candidate(1, "Nguyễn An", "old@example.com");
        candidate.AddSkill(new Skill("C#"));

        candidate.UpdateProfile(
            "new@example.com",
            "  Backend Developer  ",
            new SalaryRange(1800, 2800),
            [new Skill("PostgreSQL"), new Skill("postgresql"), new Skill("Docker")]);

        Assert.Equal("new@example.com", candidate.Email);
        Assert.Equal("Backend Developer", candidate.Bio);
        Assert.Equal(new SalaryRange(1800, 2800), candidate.DesiredSalary);
        Assert.Equal(2, candidate.Skills.Count);
        Assert.DoesNotContain(candidate.Skills, skill => skill.Name == "C#");
    }
}
