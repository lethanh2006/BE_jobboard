using JobBoard.Application.Abstractions.Authentication;
using JobBoard.Application.Authentication;
using JobBoard.Domain.Abstractions;
using JobBoard.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace JobBoard.Infrastructure.Persistence.Database;

public sealed class JobBoardDatabaseInitializer(
    JobBoardDbContext dbContext,
    IClock clock,
    IPasswordHasher passwordHasher)
{
    private readonly IClock _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    private readonly JobBoardDbContext _dbContext =
        dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    private readonly IPasswordHasher _passwordHasher =
        passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));

    public async Task InitializeAsync(
        string jobSeedFilePath,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.Database.MigrateAsync(cancellationToken);

        if (!await _dbContext.Jobs.AnyAsync(cancellationToken))
        {
            var jsonRepository = new JsonJobRepository(jobSeedFilePath, _clock);
            var jobs = await jsonRepository.GetAllAsync(cancellationToken);
            _dbContext.Jobs.AddRange(jobs.Select(job => job.ToRecord()));
        }

        if (!await _dbContext.Candidates.AnyAsync(cancellationToken))
        {
            _dbContext.Candidates.Add(CreateDemoCandidate().ToRecord());
        }

        if (!await _dbContext.UserAccounts.AnyAsync(cancellationToken))
        {
            _dbContext.UserAccounts.AddRange(CreateDemoAccounts().Select(account => account.ToRecord()));
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        if (!await _dbContext.Applications.AnyAsync(cancellationToken))
        {
            _dbContext.Applications.AddRange(CreateDemoApplications().Select(application => application.ToRecord()));
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private static Candidate CreateDemoCandidate()
    {
        var candidate = new Candidate(
            20,
            "Nguyễn Minh An",
            "an@example.com",
            "Backend Developer yêu thích sản phẩm có tác động thực tế và những hệ thống được thiết kế chỉn chu.",
            new SalaryRange(1800, 2800));
        candidate.AddSkill(new Skill("C#"));
        candidate.AddSkill(new Skill("ASP.NET Core"));
        candidate.AddSkill(new Skill("PostgreSQL"));
        candidate.AddSkill(new Skill("Docker"));
        return candidate;
    }

    private IEnumerable<UserAccount> CreateDemoAccounts()
    {
        const string demoPassword = "JobBoard@123";

        return
        [
            new UserAccount(
                20,
                "Nguyễn Minh An",
                "candidate@jobboard.vn",
                _passwordHasher.Hash(demoPassword),
                AccountRole.Candidate),
            new UserAccount(
                1,
                "Trần Bình",
                "employer@jobboard.vn",
                _passwordHasher.Hash(demoPassword),
                AccountRole.Employer,
                CompanyId: 1,
                CompanyName: "Nova Technology"),
            new UserAccount(
                100,
                "Quản trị viên",
                "admin@jobboard.vn",
                _passwordHasher.Hash(demoPassword),
                AccountRole.Admin)
        ];
    }

    private static IReadOnlyCollection<JobApplication> CreateDemoApplications() =>
    [
        JobApplication.Restore(
            1,
            2,
            20,
            new DateTime(2026, 9, 25, 8, 0, 0, DateTimeKind.Utc),
            ApplicationStatus.Reviewing),
        JobApplication.Restore(
            2,
            5,
            20,
            new DateTime(2026, 9, 21, 8, 0, 0, DateTimeKind.Utc),
            ApplicationStatus.Submitted),
        JobApplication.Restore(
            3,
            6,
            20,
            new DateTime(2026, 9, 12, 8, 0, 0, DateTimeKind.Utc),
            ApplicationStatus.Rejected)
    ];
}
