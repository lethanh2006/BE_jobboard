using Microsoft.EntityFrameworkCore;

namespace JobBoard.Infrastructure.Persistence.Database;

public sealed class JobBoardDbContext(DbContextOptions<JobBoardDbContext> options) : DbContext(options)
{
    public DbSet<JobRecord> Jobs => Set<JobRecord>();

    public DbSet<CandidateRecord> Candidates => Set<CandidateRecord>();

    public DbSet<JobApplicationRecord> Applications => Set<JobApplicationRecord>();

    public DbSet<UserAccountRecord> UserAccounts => Set<UserAccountRecord>();

    public DbSet<RefreshTokenRecord> RefreshTokens => Set<RefreshTokenRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasSequence<int>("job_ids").StartsAt(7);
        modelBuilder.HasSequence<int>("application_ids").StartsAt(4);

        modelBuilder.Entity<JobRecord>(entity =>
        {
            entity.ToTable("jobs");
            entity.HasKey(job => job.Id);
            entity.Property(job => job.Id).ValueGeneratedNever();
            entity.Property(job => job.Title).HasMaxLength(200);
            entity.Property(job => job.CompanyName).HasMaxLength(200);
            entity.Property(job => job.CompanyLocation).HasMaxLength(200);
            entity.Property(job => job.SalaryMin).HasPrecision(12, 2);
            entity.Property(job => job.SalaryMax).HasPrecision(12, 2);
            entity.Property(job => job.Location).HasMaxLength(200);
            entity.Property(job => job.Level).HasConversion<string>().HasMaxLength(32);
            entity.Property(job => job.WorkMode).HasConversion<string>().HasMaxLength(32);
            entity.Property(job => job.Category).HasMaxLength(120);
            entity.Property(job => job.Status).HasConversion<string>().HasMaxLength(32);
            entity.HasIndex(job => new { job.Status, job.Deadline });
            entity.HasIndex(job => job.CompanyId);
        });

        modelBuilder.Entity<CandidateRecord>(entity =>
        {
            entity.ToTable("candidates");
            entity.HasKey(candidate => candidate.Id);
            entity.Property(candidate => candidate.Id).ValueGeneratedNever();
            entity.Property(candidate => candidate.Name).HasMaxLength(160);
            entity.Property(candidate => candidate.Email).HasMaxLength(320);
            entity.Property(candidate => candidate.DesiredSalaryMin).HasPrecision(12, 2);
            entity.Property(candidate => candidate.DesiredSalaryMax).HasPrecision(12, 2);
            entity.HasIndex(candidate => candidate.Email).IsUnique();
        });

        modelBuilder.Entity<JobApplicationRecord>(entity =>
        {
            entity.ToTable("job_applications");
            entity.HasKey(application => application.Id);
            entity.Property(application => application.Id).ValueGeneratedNever();
            entity.Property(application => application.Status).HasConversion<string>().HasMaxLength(32);
            entity.HasIndex(application => new { application.CandidateId, application.JobId }).IsUnique();
            entity.HasIndex(application => application.JobId);
            entity.HasOne<JobRecord>()
                .WithMany()
                .HasForeignKey(application => application.JobId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<CandidateRecord>()
                .WithMany()
                .HasForeignKey(application => application.CandidateId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<UserAccountRecord>(entity =>
        {
            entity.ToTable("user_accounts");
            entity.HasKey(account => account.Id);
            entity.Property(account => account.Id).ValueGeneratedNever();
            entity.Property(account => account.Name).HasMaxLength(160);
            entity.Property(account => account.Email).HasMaxLength(320);
            entity.Property(account => account.PasswordHash).HasMaxLength(512);
            entity.Property(account => account.Role).HasConversion<string>().HasMaxLength(32);
            entity.Property(account => account.CompanyName).HasMaxLength(200);
            entity.HasIndex(account => account.Email).IsUnique();
        });

        modelBuilder.Entity<RefreshTokenRecord>(entity =>
        {
            entity.ToTable("refresh_tokens");
            entity.HasKey(token => token.Id);
            entity.Property(token => token.TokenHash).HasMaxLength(64).IsFixedLength();
            entity.Property(token => token.ReplacedByTokenHash).HasMaxLength(64).IsFixedLength();
            entity.HasIndex(token => token.TokenHash).IsUnique();
            entity.HasIndex(token => token.FamilyId);
            entity.HasIndex(token => new { token.UserId, token.ExpiresAt });
            entity.HasOne<UserAccountRecord>()
                .WithMany()
                .HasForeignKey(token => token.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
