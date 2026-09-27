using JobBoard.Domain.Entities;

namespace JobBoard.Domain.Tests.Entities;

public sealed class CompanyTests
{
    [Fact]
    public void Constructor_WithValidData_CreatesCompany()
    {
        var company = new Company(1, "  Công ty ABC  ", "  Hà Nội  ");

        Assert.Equal(1, company.Id);
        Assert.Equal("Công ty ABC", company.Name);
        Assert.Equal("Hà Nội", company.Location);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithEmptyName_Throws(string? name)
    {
        var exception = Assert.Throws<ArgumentException>(() => new Company(1, name!));

        Assert.Equal("name", exception.ParamName);
    }
}
