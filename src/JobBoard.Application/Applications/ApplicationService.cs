using JobBoard.Application.Abstractions.Persistence;
using JobBoard.Application.Exceptions;
using JobBoard.Domain.Abstractions;
using JobBoard.Domain.Entities;
using JobBoard.Domain.Exceptions;

namespace JobBoard.Application.Applications;

public sealed class ApplicationService
{
    private readonly IJobApplicationRepository _applications;
    private readonly IClock _clock;
    private readonly IJobRepository _jobs;

    public ApplicationService(
        IJobRepository jobs,
        IJobApplicationRepository applications,
        IClock clock)
    {
        _jobs = jobs ?? throw new ArgumentNullException(nameof(jobs));
        _applications = applications ?? throw new ArgumentNullException(nameof(applications));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    public async Task<JobApplication> ApplyAsync(
        int applicationId,
        int candidateId,
        int jobId,
        CancellationToken cancellationToken = default)
    {
        var job = await _jobs.GetByIdAsync(jobId, cancellationToken)
            ?? throw new ResourceNotFoundException($"Không tìm thấy tin tuyển dụng có mã {jobId}.");

        if (job.Status != JobStatus.Published || job.IsExpired)
        {
            throw new JobClosedException("Tin tuyển dụng đã đóng hoặc hết hạn nhận hồ sơ.");
        }

        if (await _applications.HasAppliedAsync(candidateId, jobId, cancellationToken))
        {
            throw new AlreadyAppliedException("Ứng viên đã ứng tuyển vào tin tuyển dụng này.");
        }

        var application = new JobApplication(applicationId, job.Id, candidateId, _clock.UtcNow);
        await _applications.AddAsync(application, cancellationToken);

        return application;
    }

    public async Task<JobApplication> ChangeStatusAsync(
        int applicationId,
        ApplicationStatus status,
        User manager,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(manager);

        var application = await _applications.GetByIdAsync(applicationId, cancellationToken)
            ?? throw new ResourceNotFoundException($"Không tìm thấy đơn ứng tuyển có mã {applicationId}.");
        var job = await _jobs.GetByIdAsync(application.JobId, cancellationToken)
            ?? throw new ResourceNotFoundException($"Không tìm thấy tin tuyển dụng có mã {application.JobId}.");

        application.ChangeStatus(status, manager, job);
        await _applications.UpdateAsync(application, cancellationToken);

        return application;
    }
}
