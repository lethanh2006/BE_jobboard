using JobBoard.Application.Abstractions.Persistence;
using JobBoard.Application.Applications;
using JobBoard.Application.Exceptions;
using JobBoard.Domain.Abstractions;
using JobBoard.Domain.Entities;
using JobBoard.Domain.Exceptions;

namespace JobBoard.Application.Tests.Applications;

public sealed class ApplicationServiceTests
{
    private static readonly DateTime Now = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private readonly InMemoryJobApplicationRepository _applications = new();
    private readonly MutableClock _clock = new(Now);
    private readonly InMemoryJobRepository _jobs = new();

    [Fact]
    public async Task Apply_ToPublishedJob_CreatesAndPersistsSubmittedApplication()
    {
        var job = AddJob();
        job.Publish();
        var service = CreateService();

        var application = await service.ApplyAsync(candidateId: 20, jobId: job.Id);

        Assert.Equal(job.Id, application.JobId);
        Assert.Equal(20, application.CandidateId);
        Assert.Equal(ApplicationStatus.Submitted, application.Status);
        Assert.Equal(Now, application.AppliedAt);
        Assert.Same(application, await _applications.GetByIdAsync(application.Id));
    }

    [Fact]
    public async Task Apply_WhenJobDoesNotExist_Throws()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<ResourceNotFoundException>(() => service.ApplyAsync(20, jobId: 999));
    }

    [Fact]
    public async Task Apply_ToDraftJob_Throws()
    {
        var job = AddJob();
        var service = CreateService();

        await Assert.ThrowsAsync<JobClosedException>(() => service.ApplyAsync(20, job.Id));
    }

    [Fact]
    public async Task Apply_ToExpiredJob_Throws()
    {
        var job = AddJob();
        job.Publish();
        _clock.UtcNow = Now.AddDays(8);
        var service = CreateService();

        await Assert.ThrowsAsync<JobClosedException>(() => service.ApplyAsync(20, job.Id));
    }

    [Fact]
    public async Task Apply_WhenCandidateAlreadyApplied_Throws()
    {
        var job = AddJob();
        job.Publish();
        await _applications.AddAsync(new JobApplication(1, job.Id, candidateId: 20, Now.AddDays(-1)));
        var service = CreateService();

        await Assert.ThrowsAsync<AlreadyAppliedException>(() => service.ApplyAsync(20, job.Id));
    }

    [Fact]
    public async Task ChangeStatus_ByAuthorizedEmployer_UpdatesAndPersistsApplication()
    {
        var job = AddJob();
        var application = new JobApplication(1, job.Id, candidateId: 20, Now);
        var employer = new Employer(30, "Trần Bình", "binh@example.com", new Company(job.CompanyId, "Công ty ABC"));
        await _applications.AddAsync(application);
        var service = CreateService();

        var result = await service.ChangeStatusAsync(application.Id, ApplicationStatus.Reviewing, employer);

        Assert.Same(application, result);
        Assert.Equal(ApplicationStatus.Reviewing, application.Status);
        Assert.Equal(1, _applications.UpdateCount);
    }

    [Fact]
    public async Task ChangeStatus_WhenApplicationDoesNotExist_Throws()
    {
        var service = CreateService();
        var admin = new AdminUser(1, "Quản trị viên", "admin@example.com");

        await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
            service.ChangeStatusAsync(999, ApplicationStatus.Reviewing, admin));
    }

    private ApplicationService CreateService() => new(_jobs, _applications, _clock);

    private Job AddJob()
    {
        var job = new Job(
            id: 10,
            title: "Backend Developer",
            companyId: 30,
            new SalaryRange(1000, 2000),
            Now.AddDays(7),
            _clock);

        _jobs.Seed(job);
        return job;
    }

    private sealed class InMemoryJobRepository : IJobRepository
    {
        private readonly Dictionary<int, Job> _jobs = [];

        public void Seed(Job job) => _jobs.Add(job.Id, job);

        public Task<Job?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_jobs.GetValueOrDefault(id));

        public Task<IReadOnlyCollection<Job>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<Job>>(_jobs.Values);

        public Task<int> GetNextIdAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(_jobs.Count + 1);

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

    private sealed class InMemoryJobApplicationRepository : IJobApplicationRepository
    {
        private readonly Dictionary<int, JobApplication> _applications = [];

        public int UpdateCount { get; private set; }

        public Task<int> GetNextIdAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(_applications.Count + 1);

        public Task<IReadOnlyCollection<JobApplication>> GetAllAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<JobApplication>>(_applications.Values.ToArray());

        public Task<JobApplication?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_applications.GetValueOrDefault(id));

        public Task AddAsync(JobApplication entity, CancellationToken cancellationToken = default)
        {
            _applications.Add(entity.Id, entity);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(JobApplication entity, CancellationToken cancellationToken = default)
        {
            _applications[entity.Id] = entity;
            UpdateCount++;
            return Task.CompletedTask;
        }

        public Task<bool> HasAppliedAsync(
            int candidateId,
            int jobId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_applications.Values.Any(application =>
                application.CandidateId == candidateId && application.JobId == jobId));

        public Task<IReadOnlyCollection<JobApplication>> GetByCandidateIdAsync(
            int candidateId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<JobApplication>>(_applications.Values
                .Where(application => application.CandidateId == candidateId)
                .ToArray());

        public Task<IReadOnlyCollection<JobApplication>> GetByJobIdAsync(
            int jobId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<JobApplication>>(_applications.Values
                .Where(application => application.JobId == jobId)
                .ToArray());
    }

    private sealed class MutableClock(DateTime utcNow) : IClock
    {
        public DateTime UtcNow { get; set; } = utcNow;
    }
}
