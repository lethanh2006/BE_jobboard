using JobBoard.Domain.Abstractions;
using JobBoard.Domain.Entities;
using JobBoard.Domain.Exceptions;

namespace JobBoard.Application.Applications;

public sealed class ApplicationService
{
    public JobApplication Apply(
        int applicationId,
        Candidate candidate,
        Job job,
        IEnumerable<JobApplication> existingApplications,
        IClock clock)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        ArgumentNullException.ThrowIfNull(job);
        ArgumentNullException.ThrowIfNull(existingApplications);
        ArgumentNullException.ThrowIfNull(clock);

        if (job.Status != JobStatus.Published || job.IsExpired)
        {
            throw new JobClosedException("Tin tuyển dụng đã đóng hoặc hết hạn nhận hồ sơ.");
        }

        if (existingApplications.Any(application =>
                application.JobId == job.Id && application.CandidateId == candidate.Id))
        {
            throw new AlreadyAppliedException("Ứng viên đã ứng tuyển vào tin tuyển dụng này.");
        }

        return new JobApplication(applicationId, job.Id, candidate.Id, clock.UtcNow);
    }

    public void ChangeStatus(
        JobApplication application,
        ApplicationStatus status,
        User manager,
        Job job)
    {
        ArgumentNullException.ThrowIfNull(application);

        application.ChangeStatus(status, manager, job);
    }
}
