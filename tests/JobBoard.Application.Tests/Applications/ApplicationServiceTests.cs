using JobBoard.Application.Applications;
using JobBoard.Domain.Abstractions;
using JobBoard.Domain.Entities;
using JobBoard.Domain.Exceptions;

namespace JobBoard.Application.Tests.Applications;

public sealed class ApplicationServiceTests
{
    private static readonly DateTime Now = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private readonly ApplicationService _service = new();

    [Fact]
    public void Apply_ToPublishedJob_CreatesSubmittedApplication()
    {
        var clock = new MutableClock(Now);
        var job = CreateJob(clock);
        var candidate = CreateCandidate();
        job.Publish();

        var application = _service.Apply(1, candidate, job, [], clock);

        Assert.Equal(job.Id, application.JobId);
        Assert.Equal(candidate.Id, application.CandidateId);
        Assert.Equal(ApplicationStatus.Submitted, application.Status);
        Assert.Equal(Now, application.AppliedAt);
    }

    [Fact]
    public void Apply_ToDraftJob_Throws()
    {
        var clock = new MutableClock(Now);
        var job = CreateJob(clock);

        Assert.Throws<JobClosedException>(() =>
            _service.Apply(1, CreateCandidate(), job, [], clock));
    }

    [Fact]
    public void Apply_ToExpiredJob_Throws()
    {
        var clock = new MutableClock(Now);
        var job = CreateJob(clock);
        job.Publish();
        clock.UtcNow = Now.AddDays(8);

        Assert.Throws<JobClosedException>(() =>
            _service.Apply(1, CreateCandidate(), job, [], clock));
    }

    [Fact]
    public void Apply_WhenCandidateAlreadyApplied_Throws()
    {
        var clock = new MutableClock(Now);
        var job = CreateJob(clock);
        var candidate = CreateCandidate();
        var existingApplication = new JobApplication(1, job.Id, candidate.Id, Now.AddDays(-1));
        job.Publish();

        Assert.Throws<AlreadyAppliedException>(() =>
            _service.Apply(2, candidate, job, [existingApplication], clock));
    }

    [Fact]
    public void ChangeStatus_DelegatesAuthorizationToDomain()
    {
        var clock = new MutableClock(Now);
        var job = CreateJob(clock);
        var application = new JobApplication(1, job.Id, 20, Now);
        var employer = new Employer(30, "Trần Bình", "binh@example.com", new Company(job.CompanyId, "Công ty ABC"));

        _service.ChangeStatus(application, ApplicationStatus.Reviewing, employer, job);

        Assert.Equal(ApplicationStatus.Reviewing, application.Status);
    }

    private static Candidate CreateCandidate() => new(20, "Nguyễn An", "an@example.com");

    private static Job CreateJob(IClock clock) =>
        new(
            id: 10,
            title: "Backend Developer",
            companyId: 30,
            new SalaryRange(1000, 2000),
            Now.AddDays(7),
            clock);

    private sealed class MutableClock(DateTime utcNow) : IClock
    {
        public DateTime UtcNow { get; set; } = utcNow;
    }
}
