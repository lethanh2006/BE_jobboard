using JobBoard.Domain.Abstractions;
using JobBoard.Domain.Entities;

namespace JobBoard.Domain.Tests.Entities;

public sealed class UserAuthorizationTests
{
    private static readonly DateTime Now = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Candidate_CannotManageJob()
    {
        var candidate = new Candidate(1, "Nguyễn An", "an@example.com");

        Assert.False(candidate.CanManage(CreateJob(companyId: 10)));
    }

    [Fact]
    public void Employer_CanManageJobOfOwnCompany()
    {
        var employer = new Employer(1, "Trần Bình", "binh@example.com", new Company(10, "Công ty ABC"));

        Assert.True(employer.CanManage(CreateJob(companyId: 10)));
        Assert.False(employer.CanManage(CreateJob(companyId: 20)));
    }

    [Fact]
    public void Employer_GetDisplayName_IncludesCompanyName()
    {
        var employer = new Employer(1, "Trần Bình", "binh@example.com", new Company(10, "Công ty ABC"));

        Assert.Equal("Trần Bình (Công ty ABC)", employer.GetDisplayName());
    }

    [Fact]
    public void Admin_CanManageEveryJob()
    {
        var admin = new AdminUser(1, "Quản trị viên", "admin@example.com");

        Assert.True(admin.CanManage(CreateJob(companyId: 10)));
        Assert.True(admin.CanManage(CreateJob(companyId: 20)));
    }

    private static Job CreateJob(int companyId) =>
        new(
            id: 1,
            title: "Backend Developer",
            companyId,
            new SalaryRange(1000, 2000),
            Now.AddDays(7),
            new FixedClock(Now));

    private sealed class FixedClock(DateTime utcNow) : IClock
    {
        public DateTime UtcNow { get; } = utcNow;
    }
}
