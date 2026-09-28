using JobBoard.Application.Abstractions.Persistence;
using JobBoard.Application.Exceptions;
using JobBoard.Application.Jobs;
using JobBoard.Domain.Abstractions;
using JobBoard.Domain.Entities;

namespace JobBoard.Application.Tests.Jobs;

public sealed class JobDetailServiceTests
{
    private static readonly DateTime Now = new(2026, 9, 28, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task GetByIdAsync_ReturnsCompleteJobDetails()
    {
        var repository = new StubJobRepository(CreateJob());
        var service = new JobDetailService(repository);

        var result = await service.GetByIdAsync(1);

        Assert.Equal("Backend Developer", result.Title);
        Assert.Equal("Công ty ABC", result.Company);
        Assert.Equal("Hà Nội", result.Location);
        Assert.Equal(JobLevel.Senior, result.Level);
        Assert.Equal(["C#"], result.Skills);
        Assert.Equal(["Xây dựng API"], result.Description);
    }

    [Fact]
    public async Task GetByIdAsync_WhenJobDoesNotExist_Throws()
    {
        var service = new JobDetailService(new StubJobRepository());

        await Assert.ThrowsAsync<ResourceNotFoundException>(() => service.GetByIdAsync(999));
    }

    private static Job CreateJob()
    {
        var job = new Job(
            1,
            "Backend Developer",
            10,
            new SalaryRange(1000, 2000),
            Now.AddDays(30),
            new FixedClock(Now),
            new Company(10, "Công ty ABC"),
            "Hà Nội",
            JobLevel.Senior,
            WorkMode.Hybrid,
            "Lập trình Backend",
            "Xây dựng sản phẩm",
            ["Xây dựng API"],
            ["Hai năm kinh nghiệm"],
            ["Làm việc linh hoạt"],
            Now.AddDays(-3),
            true);
        job.AddSkill(new Skill("C#"));
        job.Publish();

        return job;
    }

    private sealed class StubJobRepository(params Job[] jobs) : IJobRepository
    {
        private readonly Dictionary<int, Job> _jobs = jobs.ToDictionary(job => job.Id);

        public Task<Job?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_jobs.GetValueOrDefault(id));

        public Task<IReadOnlyCollection<Job>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<Job>>(_jobs.Values);

        public Task<int> GetNextIdAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(_jobs.Count + 1);

        public Task AddAsync(Job entity, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task UpdateAsync(Job entity, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class FixedClock(DateTime utcNow) : IClock
    {
        public DateTime UtcNow { get; } = utcNow;
    }
}
