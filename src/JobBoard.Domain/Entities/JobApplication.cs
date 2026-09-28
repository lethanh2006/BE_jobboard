using JobBoard.Domain.Exceptions;

namespace JobBoard.Domain.Entities;

public sealed class JobApplication
{
    public JobApplication(int id, int jobId, int candidateId, DateTime appliedAt)
    {
        if (jobId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(jobId), "JobId phải lớn hơn 0.");
        }

        if (candidateId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(candidateId), "CandidateId phải lớn hơn 0.");
        }

        Id = id;
        JobId = jobId;
        CandidateId = candidateId;
        AppliedAt = appliedAt;
    }

    private JobApplication(
        int id,
        int jobId,
        int candidateId,
        DateTime appliedAt,
        ApplicationStatus status)
        : this(id, jobId, candidateId, appliedAt)
    {
        if (!Enum.IsDefined(status))
        {
            throw new ArgumentOutOfRangeException(nameof(status), "Trạng thái đơn ứng tuyển không hợp lệ.");
        }

        Status = status;
    }

    public int Id { get; }

    public int JobId { get; }

    public int CandidateId { get; }

    public ApplicationStatus Status { get; private set; }

    public DateTime AppliedAt { get; }

    public static JobApplication Restore(
        int id,
        int jobId,
        int candidateId,
        DateTime appliedAt,
        ApplicationStatus status) =>
        new(id, jobId, candidateId, appliedAt, status);

    public void ChangeStatus(ApplicationStatus status, User manager, Job job)
    {
        ArgumentNullException.ThrowIfNull(manager);
        ArgumentNullException.ThrowIfNull(job);

        if (job.Id != JobId)
        {
            throw new ArgumentException("Tin tuyển dụng không thuộc đơn ứng tuyển này.", nameof(job));
        }

        if (!manager.CanManage(job))
        {
            throw new JobManagementForbiddenException("Người dùng không có quyền cập nhật đơn ứng tuyển của tin này.");
        }

        if (!Enum.IsDefined(status))
        {
            throw new ArgumentOutOfRangeException(nameof(status), "Trạng thái đơn ứng tuyển không hợp lệ.");
        }

        Status = status;
    }
}
