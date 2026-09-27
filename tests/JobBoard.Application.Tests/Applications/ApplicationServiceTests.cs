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
    public void Apply_ToPublishedJob_CreatesAndPersistsSubmittedApplication()
    {
        var job = AddJob();
        job.Publish();
        var service = CreateService();

        var application = service.Apply(applicationId: 1, candidateId: 20, jobId: job.Id);

        Assert.Equal(job.Id, application.JobId);
        Assert.Equal(20, application.CandidateId);
        Assert.Equal(ApplicationStatus.Submitted, application.Status);
        Assert.Equal(Now, application.AppliedAt);
        Assert.Same(application, _applications.GetById(application.Id));
    }

    [Fact]
    public void Apply_WhenJobDoesNotExist_Throws()
    {
        var service = CreateService();

        Assert.Throws<ResourceNotFoundException>(() => service.Apply(1, 20, jobId: 999));
    }

    [Fact]
    public void Apply_ToDraftJob_Throws()
    {
        var job = AddJob();
        var service = CreateService();

        Assert.Throws<JobClosedException>(() => service.Apply(1, 20, job.Id));
    }

    [Fact]
    public void Apply_ToExpiredJob_Throws()
    {
        var job = AddJob();
        job.Publish();
        _clock.UtcNow = Now.AddDays(8);
        var service = CreateService();

        Assert.Throws<JobClosedException>(() => service.Apply(1, 20, job.Id));
    }

    [Fact]
    public void Apply_WhenCandidateAlreadyApplied_Throws()
    {
        var job = AddJob();
        job.Publish();
        _applications.Add(new JobApplication(1, job.Id, candidateId: 20, Now.AddDays(-1)));
        var service = CreateService();

        Assert.Throws<AlreadyAppliedException>(() => service.Apply(2, 20, job.Id));
    }

    [Fact]
    public void ChangeStatus_ByAuthorizedEmployer_UpdatesAndPersistsApplication()
    {
        var job = AddJob();
        var application = new JobApplication(1, job.Id, candidateId: 20, Now);
        var employer = new Employer(30, "Trần Bình", "binh@example.com", new Company(job.CompanyId, "Công ty ABC"));
        _applications.Add(application);
        var service = CreateService();

        var result = service.ChangeStatus(application.Id, ApplicationStatus.Reviewing, employer);

        Assert.Same(application, result);
        Assert.Equal(ApplicationStatus.Reviewing, application.Status);
        Assert.Equal(1, _applications.UpdateCount);
    }

    [Fact]
    public void ChangeStatus_WhenApplicationDoesNotExist_Throws()
    {
        var service = CreateService();
        var admin = new AdminUser(1, "Quản trị viên", "admin@example.com");

        Assert.Throws<ResourceNotFoundException>(() =>
            service.ChangeStatus(999, ApplicationStatus.Reviewing, admin));
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

        _jobs.Add(job);
        return job;
    }

    private sealed class InMemoryJobRepository : IJobRepository
    {
        private readonly Dictionary<int, Job> _jobs = [];

        public Job? GetById(int id) => _jobs.GetValueOrDefault(id);

        public void Add(Job entity) => _jobs.Add(entity.Id, entity);

        public void Update(Job entity) => _jobs[entity.Id] = entity;
    }

    private sealed class InMemoryJobApplicationRepository : IJobApplicationRepository
    {
        private readonly Dictionary<int, JobApplication> _applications = [];

        public int UpdateCount { get; private set; }

        public JobApplication? GetById(int id) => _applications.GetValueOrDefault(id);

        public void Add(JobApplication entity) => _applications.Add(entity.Id, entity);

        public void Update(JobApplication entity)
        {
            _applications[entity.Id] = entity;
            UpdateCount++;
        }

        public bool HasApplied(int candidateId, int jobId) =>
            _applications.Values.Any(application =>
                application.CandidateId == candidateId && application.JobId == jobId);
    }

    private sealed class MutableClock(DateTime utcNow) : IClock
    {
        public DateTime UtcNow { get; set; } = utcNow;
    }
}
