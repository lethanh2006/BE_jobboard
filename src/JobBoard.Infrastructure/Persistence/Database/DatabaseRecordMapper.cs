using JobBoard.Application.Authentication;
using JobBoard.Domain.Abstractions;
using JobBoard.Domain.Entities;

namespace JobBoard.Infrastructure.Persistence.Database;

internal static class DatabaseRecordMapper
{
    public static Job ToDomain(this JobRecord record, IClock clock)
    {
        var job = Job.Restore(
            record.Id,
            record.Title,
            record.CompanyId,
            new SalaryRange(record.SalaryMin, record.SalaryMax),
            record.Deadline,
            clock,
            new Company(record.CompanyId, record.CompanyName, record.CompanyLocation),
            record.Location,
            record.Level,
            record.WorkMode,
            record.Category,
            record.Summary,
            record.Description,
            record.Requirements,
            record.Benefits,
            record.PostedAt,
            record.Featured,
            record.Status);

        foreach (var skill in record.Skills)
        {
            job.AddSkill(new Skill(skill));
        }

        return job;
    }

    public static JobRecord ToRecord(this Job job) =>
        new()
        {
            Id = job.Id,
            Title = job.Title,
            CompanyId = job.CompanyId,
            CompanyName = job.Company?.Name ?? $"Công ty #{job.CompanyId}",
            CompanyLocation = job.Company?.Location,
            SalaryMin = job.Salary.Min,
            SalaryMax = job.Salary.Max,
            Deadline = job.Deadline,
            Location = job.Location,
            Level = job.Level,
            WorkMode = job.WorkMode,
            Category = job.Category,
            Summary = job.Summary,
            Description = job.Description.ToArray(),
            Requirements = job.Requirements.ToArray(),
            Benefits = job.Benefits.ToArray(),
            PostedAt = job.PostedAt,
            Featured = job.Featured,
            Status = job.Status,
            Skills = job.Skills.Select(skill => skill.Name).ToArray()
        };

    public static void UpdateFrom(this JobRecord record, Job job)
    {
        var updated = job.ToRecord();
        record.Title = updated.Title;
        record.CompanyId = updated.CompanyId;
        record.CompanyName = updated.CompanyName;
        record.CompanyLocation = updated.CompanyLocation;
        record.SalaryMin = updated.SalaryMin;
        record.SalaryMax = updated.SalaryMax;
        record.Deadline = updated.Deadline;
        record.Location = updated.Location;
        record.Level = updated.Level;
        record.WorkMode = updated.WorkMode;
        record.Category = updated.Category;
        record.Summary = updated.Summary;
        record.Description = updated.Description;
        record.Requirements = updated.Requirements;
        record.Benefits = updated.Benefits;
        record.PostedAt = updated.PostedAt;
        record.Featured = updated.Featured;
        record.Status = updated.Status;
        record.Skills = updated.Skills;
    }

    public static Candidate ToDomain(this CandidateRecord record)
    {
        var desiredSalary = record.DesiredSalaryMin is decimal min &&
            record.DesiredSalaryMax is decimal max
                ? new SalaryRange(min, max)
                : null;
        var candidate = new Candidate(
            record.Id,
            record.Name,
            record.Email,
            record.Bio,
            desiredSalary);

        foreach (var skill in record.Skills)
        {
            candidate.AddSkill(new Skill(skill));
        }

        return candidate;
    }

    public static CandidateRecord ToRecord(this Candidate candidate) =>
        new()
        {
            Id = candidate.Id,
            Name = candidate.Name,
            Email = candidate.Email,
            Bio = candidate.Bio,
            DesiredSalaryMin = candidate.DesiredSalary?.Min,
            DesiredSalaryMax = candidate.DesiredSalary?.Max,
            Skills = candidate.Skills.Select(skill => skill.Name).ToArray()
        };

    public static void UpdateFrom(this CandidateRecord record, Candidate candidate)
    {
        var updated = candidate.ToRecord();
        record.Name = updated.Name;
        record.Email = updated.Email;
        record.Bio = updated.Bio;
        record.DesiredSalaryMin = updated.DesiredSalaryMin;
        record.DesiredSalaryMax = updated.DesiredSalaryMax;
        record.Skills = updated.Skills;
    }

    public static JobApplication ToDomain(this JobApplicationRecord record) =>
        JobApplication.Restore(
            record.Id,
            record.JobId,
            record.CandidateId,
            record.AppliedAt,
            record.Status);

    public static JobApplicationRecord ToRecord(this JobApplication application) =>
        new()
        {
            Id = application.Id,
            JobId = application.JobId,
            CandidateId = application.CandidateId,
            AppliedAt = application.AppliedAt,
            Status = application.Status
        };

    public static UserAccount ToDomain(this UserAccountRecord record) =>
        new(
            record.Id,
            record.Name,
            record.Email,
            record.PasswordHash,
            record.Role,
            record.CompanyId,
            record.CompanyName);

    public static UserAccountRecord ToRecord(this UserAccount account) =>
        new()
        {
            Id = account.Id,
            Name = account.Name,
            Email = account.Email,
            PasswordHash = account.PasswordHash,
            Role = account.Role,
            CompanyId = account.CompanyId,
            CompanyName = account.CompanyName
        };
}
