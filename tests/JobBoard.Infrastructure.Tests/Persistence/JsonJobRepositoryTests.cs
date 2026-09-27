using JobBoard.Domain.Abstractions;
using JobBoard.Domain.Entities;
using JobBoard.Infrastructure.Persistence;

namespace JobBoard.Infrastructure.Tests.Persistence;

public sealed class JsonJobRepositoryTests
{
    private static readonly DateTime Now = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task GetAllAsync_LoadsJobsFromSeedFile()
    {
        var repository = CreateRepository();

        var jobs = await repository.GetAllAsync();

        Assert.Equal(3, jobs.Count);
        var backendJob = Assert.Single(jobs, job => job.Id == 1);
        Assert.Equal("Backend Developer (.NET)", backendJob.Title);
        Assert.Equal(JobStatus.Published, backendJob.Status);
        Assert.Contains(backendJob.Skills, skill => skill.Name == "PostgreSQL");
    }

    [Fact]
    public async Task GetByIdAsync_AfterFirstRead_UsesLoadedData()
    {
        var repository = CreateRepository();
        await repository.GetAllAsync();

        var job = await repository.GetByIdAsync(2);

        Assert.NotNull(job);
        Assert.Equal("Frontend Developer (Next.js)", job.Title);
    }

    [Fact]
    public async Task AddAndUpdateAsync_ChangesInMemoryCollection()
    {
        var repository = CreateRepository();
        var job = new Job(
            4,
            "QA Engineer",
            2,
            new SalaryRange(1200, 2000),
            Now.AddDays(30),
            new FixedClock(Now));

        await repository.AddAsync(job);
        job.Publish();
        await repository.UpdateAsync(job);

        var savedJob = await repository.GetByIdAsync(4);
        Assert.NotNull(savedJob);
        Assert.Equal(JobStatus.Published, savedJob.Status);
    }

    private static JsonJobRepository CreateRepository()
    {
        var seedPath = Path.Combine(AppContext.BaseDirectory, "Data", "Seed", "jobs.json");
        return new JsonJobRepository(seedPath, new FixedClock(Now));
    }

    private sealed class FixedClock(DateTime utcNow) : IClock
    {
        public DateTime UtcNow { get; } = utcNow;
    }
}
