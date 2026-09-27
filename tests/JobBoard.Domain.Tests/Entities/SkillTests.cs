using JobBoard.Domain.Entities;

namespace JobBoard.Domain.Tests.Entities;

public sealed class SkillTests
{
    [Fact]
    public void Constructor_WithValidName_TrimsName()
    {
        var skill = new Skill("  C#  ");

        Assert.Equal("C#", skill.Name);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithEmptyName_Throws(string? name)
    {
        var exception = Assert.Throws<ArgumentException>(() => new Skill(name!));

        Assert.Equal("name", exception.ParamName);
    }
}