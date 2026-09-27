using JobBoard.Application.Abstractions.Persistence;
using JobBoard.Application.Jobs;
using JobBoard.Domain.Abstractions;
using JobBoard.Domain.Entities;

namespace JobBoard.Application.Tests.Jobs;

public sealed class JobSearchServiceTests
{
    private static readonly DateTime Now = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private readonly InMemoryJobRepository _jobs = new();

    [Fact]
    public void Search_AppliesStatusKeywordSalaryAndSkillFilters()
    {
        AddJob(1, "Senior Backend Developer", 1500, 2500, true, "C#", "PostgreSQL");
        AddJob(2, "Junior Backend Developer", 800, 1200, true, "C#");
        AddJob(3, "Frontend Developer", 1800, 2800, true, "TypeScript");
        AddJob(4, "Backend Architect", 3000, 5000, false, "C#");
        var service = new JobSearchService(_jobs);

        var result = service.Search(new SearchJobsQuery(
            Keyword: " backend ",
            MinimumSalary: 1000,
            Skills: [" c# "]));

        var job = Assert.Single(result.Items);
        Assert.Equal(1, job.Id);
        Assert.Equal("Senior Backend Developer", job.Title);
        Assert.Equal(["C#", "PostgreSQL"], job.Skills);
        Assert.Equal(1, result.TotalItems);
        Assert.Equal(1, result.TotalPages);
    }

    [Fact]
    public void Search_OrdersByMaximumSalaryAndPaginates()
    {
        AddJob(1, "Job 1", 1000, 2000, true);
        AddJob(2, "Job 2", 1000, 3000, true);
        AddJob(3, "Job 3", 1000, 1000, true);
        var service = new JobSearchService(_jobs);

        var result = service.Search(new SearchJobsQuery(Page: 2, PageSize: 1));

        Assert.Equal(1, Assert.Single(result.Items).Id);
        Assert.Equal(3, result.TotalItems);
        Assert.Equal(3, result.TotalPages);
    }

    [Fact]
    public void Search_ExcludesExpiredJob()
    {
        var clock = new MutableClock(Now);
        var job = CreateJob(1, "Backend Developer", 1000, 2000, clock);
        job.Publish();
        _jobs.Add(job);
        clock.UtcNow = Now.AddDays(8);
        var service = new JobSearchService(_jobs);

        var result = service.Search(new SearchJobsQuery());

        Assert.Empty(result.Items);
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public void Search_WithInvalidPagination_Throws(int page, int pageSize)
    {
        var service = new JobSearchService(_jobs);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            service.Search(new SearchJobsQuery(Page: page, PageSize: pageSize)));
    }

    private void AddJob(
        int id,
        string title,
        decimal salaryMin,
        decimal salaryMax,
        bool publish,
        params string[] skills)
    {
        var job = CreateJob(id, title, salaryMin, salaryMax, new MutableClock(Now));
        foreach (var skill in skills)
        {
            job.AddSkill(new Skill(skill));
        }

        if (publish)
        {
            job.Publish();
        }

        _jobs.Add(job);
    }

    private static Job CreateJob(
        int id,
        string title,
        decimal salaryMin,
        decimal salaryMax,
        IClock clock) =>
        new(id, title, companyId: 30, new SalaryRange(salaryMin, salaryMax), Now.AddDays(7), clock);

    private sealed class InMemoryJobRepository : IJobRepository
    {
        private readonly Dictionary<int, Job> _jobs = [];

        public Job? GetById(int id) => _jobs.GetValueOrDefault(id);

        public IReadOnlyCollection<Job> GetAll() => _jobs.Values;

        public void Add(Job entity) => _jobs.Add(entity.Id, entity);

        public void Update(Job entity) => _jobs[entity.Id] = entity;
    }

    private sealed class MutableClock(DateTime utcNow) : IClock
    {
        public DateTime UtcNow { get; set; } = utcNow;
    }
}
