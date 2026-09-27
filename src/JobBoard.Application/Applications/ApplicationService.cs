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

    public JobApplication Apply(int applicationId, int candidateId, int jobId)
    {
        var job = _jobs.GetById(jobId)
            ?? throw new ResourceNotFoundException($"Không tìm thấy tin tuyển dụng có mã {jobId}.");

        if (job.Status != JobStatus.Published || job.IsExpired)
        {
            throw new JobClosedException("Tin tuyển dụng đã đóng hoặc hết hạn nhận hồ sơ.");
        }

        if (_applications.HasApplied(candidateId, jobId))
        {
            throw new AlreadyAppliedException("Ứng viên đã ứng tuyển vào tin tuyển dụng này.");
        }

        var application = new JobApplication(applicationId, job.Id, candidateId, _clock.UtcNow);
        _applications.Add(application);

        return application;
    }

    public JobApplication ChangeStatus(int applicationId, ApplicationStatus status, User manager)
    {
        ArgumentNullException.ThrowIfNull(manager);

        var application = _applications.GetById(applicationId)
            ?? throw new ResourceNotFoundException($"Không tìm thấy đơn ứng tuyển có mã {applicationId}.");
        var job = _jobs.GetById(application.JobId)
            ?? throw new ResourceNotFoundException($"Không tìm thấy tin tuyển dụng có mã {application.JobId}.");

        application.ChangeStatus(status, manager, job);
        _applications.Update(application);

        return application;
    }
}
