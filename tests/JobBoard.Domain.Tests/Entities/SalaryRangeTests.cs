using JobBoard.Domain.Entities;

namespace JobBoard.Domain.Tests.Entities;

public sealed class SalaryRangeTests
{
    [Fact]
    public void Constructor_WithValidRange_CreatesValueObject()
    {
        var salary = new SalaryRange(1000, 2000);

        Assert.Equal(1000, salary.Min);
        Assert.Equal(2000, salary.Max);
    }

    [Fact]
    public void Constructor_WithNegativeMinimum_Throws()
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => new SalaryRange(-1, 2000));

        Assert.Equal("min", exception.ParamName);
    }

    [Fact]
    public void Constructor_WhenMinimumIsGreaterThanMaximum_Throws()
    {
        var exception = Assert.Throws<ArgumentException>(() => new SalaryRange(2000, 1000));

        Assert.Equal("max", exception.ParamName);
    }

    [Theory]
    [InlineData(1000, 2000, 1500, 2500, true)]
    [InlineData(1000, 2000, 2000, 3000, true)]
    [InlineData(1000, 2000, 2001, 3000, false)]
    public void Overlaps_ReturnsExpectedResult(
        decimal firstMin,
        decimal firstMax,
        decimal secondMin,
        decimal secondMax,
        bool expected)
    {
        var first = new SalaryRange(firstMin, firstMax);
        var second = new SalaryRange(secondMin, secondMax);

        Assert.Equal(expected, first.Overlaps(second));
    }
}
