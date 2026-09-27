using JobBoard.Domain.Abstractions;

namespace JobBoard.Domain.Entities;

public sealed class Job
{
    public Job(int id, string title, int companyId, DateTime deadline, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);

        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Tiêu đề công việc không được để trống.", nameof(title));
        }

        if (companyId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(companyId), "CompanyId phải lớn hơn 0.");
        }

        if (deadline <= clock.UtcNow)
        {
            throw new ArgumentException("Hạn nộp hồ sơ phải ở tương lai.", nameof(deadline));
        }

        Id = id;
        Title = title.Trim();
        CompanyId = companyId;
        Deadline = deadline;
    }

    public int Id { get; }

    public string Title { get; }

    public int CompanyId { get; }

    public DateTime Deadline { get; }
}
