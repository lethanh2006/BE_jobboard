using JobBoard.Domain.Abstractions;
using JobBoard.Domain.Entities;

namespace JobBoard.Domain.Tests.Entities;

public sealed class JobTests
{
    private static readonly DateTime Now = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Constructor_WithValidData_CreatesJob()
    {
        var job = new Job(1, "  Backend Developer  ", 10, Now.AddDays(7), new FixedClock(Now));

        Assert.Equal(1, job.Id);
        Assert.Equal("Backend Developer", job.Title);
        Assert.Equal(10, job.CompanyId);
        Assert.Equal(Now.AddDays(7), job.Deadline);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithEmptyTitle_Throws(string? title)
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            new Job(1, title!, 10, Now.AddDays(7), new FixedClock(Now)));

        Assert.Equal("title", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithInvalidCompanyId_Throws()
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Job(1, "Backend Developer", 0, Now.AddDays(7), new FixedClock(Now)));

        Assert.Equal("companyId", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithDeadlineInThePast_Throws()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            new Job(1, "Backend Developer", 10, Now, new FixedClock(Now)));

        Assert.Equal("deadline", exception.ParamName);
    }

    private sealed class FixedClock(DateTime utcNow) : IClock
    {
        public DateTime UtcNow { get; } = utcNow;
    }
}
