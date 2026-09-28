using JobBoard.Domain.Abstractions;
using JobBoard.Domain.Entities;
using JobBoard.Domain.Exceptions;

namespace JobBoard.Domain.Tests.Entities;

public sealed class JobApplicationTests
{
    private static readonly DateTime Now = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Constructor_WithValidData_CreatesSubmittedApplication()
    {
        var application = new JobApplication(1, 10, 20, Now);

        Assert.Equal(1, application.Id);
        Assert.Equal(10, application.JobId);
        Assert.Equal(20, application.CandidateId);
        Assert.Equal(ApplicationStatus.Submitted, application.Status);
        Assert.Equal(Now, application.AppliedAt);
    }

    [Fact]
    public void Restore_WithPersistedStatus_RehydratesApplication()
    {
        var application = JobApplication.Restore(
            1,
            10,
            20,
            Now,
            ApplicationStatus.Reviewing);

        Assert.Equal(ApplicationStatus.Reviewing, application.Status);
    }

    [Fact]
    public void ChangeStatus_ByEmployerOfOwningCompany_UpdatesStatus()
    {
        var job = CreateJob(id: 10, companyId: 30);
        var employer = new Employer(1, "Trần Bình", "binh@example.com", new Company(30, "Công ty ABC"));
        var application = new JobApplication(1, job.Id, 20, Now);

        application.ChangeStatus(ApplicationStatus.Reviewing, employer, job);

        Assert.Equal(ApplicationStatus.Reviewing, application.Status);
    }

    [Fact]
    public void ChangeStatus_ByEmployerOfAnotherCompany_Throws()
    {
        var job = CreateJob(id: 10, companyId: 30);
        var employer = new Employer(1, "Trần Bình", "binh@example.com", new Company(40, "Công ty XYZ"));
        var application = new JobApplication(1, job.Id, 20, Now);

        Assert.Throws<JobManagementForbiddenException>(() =>
            application.ChangeStatus(ApplicationStatus.Rejected, employer, job));
    }

    [Fact]
    public void ChangeStatus_ByAdmin_UpdatesStatus()
    {
        var job = CreateJob(id: 10, companyId: 30);
        var admin = new AdminUser(1, "Quản trị viên", "admin@example.com");
        var application = new JobApplication(1, job.Id, 20, Now);

        application.ChangeStatus(ApplicationStatus.Accepted, admin, job);

        Assert.Equal(ApplicationStatus.Accepted, application.Status);
    }

    [Fact]
    public void ChangeStatus_WithAnotherJob_Throws()
    {
        var application = new JobApplication(1, 10, 20, Now);
        var admin = new AdminUser(1, "Quản trị viên", "admin@example.com");

        var exception = Assert.Throws<ArgumentException>(() =>
            application.ChangeStatus(ApplicationStatus.Reviewing, admin, CreateJob(id: 11, companyId: 30)));

        Assert.Equal("job", exception.ParamName);
    }

    private static Job CreateJob(int id, int companyId) =>
        new(
            id,
            "Backend Developer",
            companyId,
            new SalaryRange(1000, 2000),
            Now.AddDays(7),
            new FixedClock(Now));

    private sealed class FixedClock(DateTime utcNow) : IClock
    {
        public DateTime UtcNow { get; } = utcNow;
    }
}
