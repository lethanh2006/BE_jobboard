using JobBoard.Application.Authentication;
using JobBoard.Domain.Entities;

namespace JobBoard.Infrastructure.Persistence.Database;

public sealed class JobRecord
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public int CompanyId { get; set; }

    public string CompanyName { get; set; } = string.Empty;

    public string? CompanyLocation { get; set; }

    public decimal SalaryMin { get; set; }

    public decimal SalaryMax { get; set; }

    public DateTime Deadline { get; set; }

    public string Location { get; set; } = string.Empty;

    public JobLevel Level { get; set; }

    public WorkMode WorkMode { get; set; }

    public string Category { get; set; } = string.Empty;

    public string Summary { get; set; } = string.Empty;

    public string[] Description { get; set; } = [];

    public string[] Requirements { get; set; } = [];

    public string[] Benefits { get; set; } = [];

    public DateTime PostedAt { get; set; }

    public bool Featured { get; set; }

    public JobStatus Status { get; set; }

    public string[] Skills { get; set; } = [];
}

public sealed class CandidateRecord
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string? Bio { get; set; }

    public decimal? DesiredSalaryMin { get; set; }

    public decimal? DesiredSalaryMax { get; set; }

    public string[] Skills { get; set; } = [];
}

public sealed class JobApplicationRecord
{
    public int Id { get; set; }

    public int JobId { get; set; }

    public int CandidateId { get; set; }

    public ApplicationStatus Status { get; set; }

    public DateTime AppliedAt { get; set; }
}

public sealed class UserAccountRecord
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public AccountRole Role { get; set; }

    public int? CompanyId { get; set; }

    public string? CompanyName { get; set; }
}

public sealed class RefreshTokenRecord
{
    public Guid Id { get; set; }

    public int UserId { get; set; }

    public Guid FamilyId { get; set; }

    public string TokenHash { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime ExpiresAt { get; set; }

    public DateTime? RevokedAt { get; set; }

    public string? ReplacedByTokenHash { get; set; }
}
