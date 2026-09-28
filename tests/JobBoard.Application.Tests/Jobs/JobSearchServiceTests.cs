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
    public async Task Search_AppliesStatusKeywordSalaryAndSkillFilters()
    {
        AddJob(1, "Senior Backend Developer", 1500, 2500, true, "C#", "PostgreSQL");
        AddJob(2, "Junior Backend Developer", 800, 1200, true, "C#");
        AddJob(3, "Frontend Developer", 1800, 2800, true, "TypeScript");
        AddJob(4, "Backend Architect", 3000, 5000, false, "C#");
        var service = new JobSearchService(_jobs);

        var result = await service.SearchAsync(new SearchJobsQuery(
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
    public async Task Search_OrdersByMaximumSalaryAndPaginates()
    {
        AddJob(1, "Job 1", 1000, 2000, true);
        AddJob(2, "Job 2", 1000, 3000, true);
        AddJob(3, "Job 3", 1000, 1000, true);
        var service = new JobSearchService(_jobs);

        var result = await service.SearchAsync(new SearchJobsQuery(
            Page: 2,
            PageSize: 1,
            Sort: JobSortOrder.SalaryDescending));

        Assert.Equal(1, Assert.Single(result.Items).Id);
        Assert.Equal(3, result.TotalItems);
        Assert.Equal(3, result.TotalPages);
    }

    [Fact]
    public async Task Search_ExcludesExpiredJob()
    {
        var clock = new MutableClock(Now);
        var job = CreateJob(1, "Backend Developer", 1000, 2000, clock);
        job.Publish();
        _jobs.Seed(job);
        clock.UtcNow = Now.AddDays(8);
        var service = new JobSearchService(_jobs);

        var result = await service.SearchAsync(new SearchJobsQuery());

        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task Search_AppliesLocationCategoryAndLevelFilters()
    {
        var matchingJob = new Job(
            1,
            "Backend Developer",
            30,
            new SalaryRange(1000, 2000),
            Now.AddDays(7),
            new MutableClock(Now),
            new Company(30, "Công ty ABC"),
            "Hà Nội",
            JobLevel.Senior,
            WorkMode.Hybrid,
            "Lập trình Backend");
        matchingJob.Publish();
        _jobs.Seed(matchingJob);
        AddJob(2, "Frontend Developer", 1000, 2000, true, "TypeScript");
        var service = new JobSearchService(_jobs);

        var result = await service.SearchAsync(new SearchJobsQuery(
            Location: "hà nội",
            Category: "backend",
            Level: JobLevel.Senior));

        Assert.Equal(1, Assert.Single(result.Items).Id);
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public async Task Search_WithInvalidPagination_Throws(int page, int pageSize)
    {
        var service = new JobSearchService(_jobs);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            service.SearchAsync(new SearchJobsQuery(Page: page, PageSize: pageSize)));
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

        _jobs.Seed(job);
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

        public void Seed(Job job) => _jobs.Add(job.Id, job);

        public Task<Job?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_jobs.GetValueOrDefault(id));

        public Task<IReadOnlyCollection<Job>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<Job>>(_jobs.Values);

        public Task AddAsync(Job entity, CancellationToken cancellationToken = default)
        {
            _jobs.Add(entity.Id, entity);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Job entity, CancellationToken cancellationToken = default)
        {
            _jobs[entity.Id] = entity;
            return Task.CompletedTask;
        }
    }

    private sealed class MutableClock(DateTime utcNow) : IClock
    {
        public DateTime UtcNow { get; set; } = utcNow;
    }
}
