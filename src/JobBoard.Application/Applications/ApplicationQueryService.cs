using JobBoard.Application.Abstractions.Persistence;

namespace JobBoard.Application.Applications;

public sealed class ApplicationQueryService(
    IJobRepository jobs,
    IJobApplicationRepository applications)
{
    private readonly IJobApplicationRepository _applications =
        applications ?? throw new ArgumentNullException(nameof(applications));
    private readonly IJobRepository _jobs = jobs ?? throw new ArgumentNullException(nameof(jobs));

    public async Task<IReadOnlyList<ApplicationDto>> GetByCandidateIdAsync(
        int candidateId,
        CancellationToken cancellationToken = default)
    {
        var candidateApplications = await _applications.GetByCandidateIdAsync(candidateId, cancellationToken);
        var jobsById = (await _jobs.GetAllAsync(cancellationToken)).ToDictionary(job => job.Id);

        return candidateApplications
            .Where(application => jobsById.ContainsKey(application.JobId))
            .Select(application => Map(application, jobsById[application.JobId]))
            .OrderByDescending(application => application.AppliedAt)
            .ToList();
    }

    public async Task<IReadOnlyList<ApplicationDto>> GetByCompanyIdAsync(
        int companyId,
        CancellationToken cancellationToken = default)
    {
        var companyJobs = (await _jobs.GetAllAsync(cancellationToken))
            .Where(job => job.CompanyId == companyId)
            .ToDictionary(job => job.Id);
        var allApplications = await _applications.GetAllAsync(cancellationToken);

        return allApplications
            .Where(application => companyJobs.ContainsKey(application.JobId))
            .Select(application => Map(application, companyJobs[application.JobId]))
            .OrderByDescending(application => application.AppliedAt)
            .ToList();
    }

    private static ApplicationDto Map(
        Domain.Entities.JobApplication application,
        Domain.Entities.Job job) =>
        new(
            application.Id,
            job.Id,
            application.CandidateId,
            GetCandidateName(application.CandidateId),
            job.Title,
            job.Company?.Name ?? $"Công ty #{job.CompanyId}",
            job.Location,
            application.Status,
            application.AppliedAt);

    private static string GetCandidateName(int candidateId) => candidateId switch
    {
        20 => "Nguyễn Minh An",
        21 => "Trần Hoàng Linh",
        22 => "Lê Thu Trang",
        _ => $"Ứng viên #{candidateId}"
    };
}
