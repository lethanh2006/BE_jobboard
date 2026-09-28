using JobBoard.Domain.Abstractions;
using JobBoard.Domain.Entities;
using JobBoard.Domain.Exceptions;

namespace JobBoard.Domain.Tests.Entities;

public sealed class JobTests
{
    private static readonly DateTime Now = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Constructor_WithValidData_CreatesJob()
    {
        var salary = new SalaryRange(1000, 2000);
        var job = new Job(1, "  Backend Developer  ", 10, salary, Now.AddDays(7), new FixedClock(Now));

        Assert.Equal(1, job.Id);
        Assert.Equal("Backend Developer", job.Title);
        Assert.Equal(10, job.CompanyId);
        Assert.Equal(salary, job.Salary);
        Assert.Equal(Now.AddDays(7), job.Deadline);
        Assert.Equal(JobStatus.Draft, job.Status);
        Assert.False(job.IsExpired);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithEmptyTitle_Throws(string? title)
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            new Job(1, title!, 10, new SalaryRange(1000, 2000), Now.AddDays(7), new FixedClock(Now)));

        Assert.Equal("title", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithInvalidCompanyId_Throws()
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Job(1, "Backend Developer", 0, new SalaryRange(1000, 2000), Now.AddDays(7), new FixedClock(Now)));

        Assert.Equal("companyId", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithDeadlineInThePast_Throws()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            new Job(1, "Backend Developer", 10, new SalaryRange(1000, 2000), Now, new FixedClock(Now)));

        Assert.Equal("deadline", exception.ParamName);
    }

    [Fact]
    public void Publish_ChangesDraftToPublished()
    {
        var job = CreateJob();

        job.Publish();

        Assert.Equal(JobStatus.Published, job.Status);
    }

    [Fact]
    public void Publish_RaisesPublishedEventOnce()
    {
        var job = CreateJob();
        var publishedJobs = new List<Job>();
        job.Published += publishedJobs.Add;

        job.Publish();

        Assert.Equal([job], publishedJobs);
    }

    [Fact]
    public void Close_ChangesPublishedToClosed()
    {
        var job = CreateJob();
        job.Publish();

        job.Close();

        Assert.Equal(JobStatus.Closed, job.Status);
    }

    [Fact]
    public void Publish_WhenAlreadyPublished_Throws()
    {
        var job = CreateJob();
        job.Publish();

        Assert.Throws<InvalidJobStateException>(() => job.Publish());
    }

    [Fact]
    public void Close_WhenDraft_Throws()
    {
        var job = CreateJob();

        Assert.Throws<InvalidJobStateException>(() => job.Close());
    }

    [Fact]
    public void AddSkill_WithSameNameIgnoringCase_DoesNotAddDuplicate()
    {
        var job = CreateJob();

        var firstAdded = job.AddSkill(new Skill("C#"));
        var secondAdded = job.AddSkill(new Skill(" c# "));

        Assert.True(firstAdded);
        Assert.False(secondAdded);
        Assert.Single(job.Skills);
    }

    [Fact]
    public void AddSkill_WhenLimitIsReached_Throws()
    {
        var job = CreateJob();

        for (var index = 1; index <= Job.MaxSkillsPerJob; index++)
        {
            job.AddSkill(new Skill($"Skill {index}"));
        }

        Assert.Throws<InvalidOperationException>(() => job.AddSkill(new Skill("Skill 16")));
    }

    [Fact]
    public void IsExpired_WhenDeadlineIsInThePast_ReturnsTrue()
    {
        var clock = new MutableClock(Now);
        var job = new Job(1, "Backend Developer", 10, new SalaryRange(1000, 2000), Now.AddDays(1), clock);

        clock.UtcNow = Now.AddDays(1).AddSeconds(1);

        Assert.True(job.IsExpired);
    }

    [Fact]
    public void Restore_WithExpiredPublishedJob_PreservesHistoricalState()
    {
        var job = Job.Restore(
            1,
            "Backend Developer",
            10,
            new SalaryRange(1000, 2000),
            Now.AddDays(-1),
            new FixedClock(Now),
            new Company(10, "Nova Technology"),
            "TP. Hồ Chí Minh",
            JobLevel.Middle,
            WorkMode.Hybrid,
            "Backend",
            "Summary",
            [],
            [],
            [],
            Now.AddDays(-10),
            false,
            JobStatus.Published);

        Assert.Equal(JobStatus.Published, job.Status);
        Assert.True(job.IsExpired);
    }

    private static Job CreateJob() =>
        new(1, "Backend Developer", 10, new SalaryRange(1000, 2000), Now.AddDays(7), new FixedClock(Now));

    private sealed class FixedClock(DateTime utcNow) : IClock
    {
        public DateTime UtcNow { get; } = utcNow;
    }

    private sealed class MutableClock(DateTime utcNow) : IClock
    {
        public DateTime UtcNow { get; set; } = utcNow;
    }
}
