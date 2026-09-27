using JobBoard.Domain.Abstractions;
using JobBoard.Domain.Exceptions;

namespace JobBoard.Domain.Entities;

public sealed class Job
{
    public const int MaxSkillsPerJob = 15;

    private readonly IClock _clock;
    private readonly HashSet<Skill> _skills = new(SkillNameComparer.Instance);

    public Job(
        int id,
        string title,
        int companyId,
        SalaryRange salary,
        DateTime deadline,
        IClock clock)
    {
        ArgumentNullException.ThrowIfNull(salary);
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
        Salary = salary;
        Deadline = deadline;
        _clock = clock;
    }

    public int Id { get; }

    public string Title { get; }

    public int CompanyId { get; }

    public SalaryRange Salary { get; }

    public DateTime Deadline { get; }

    public JobStatus Status { get; private set; }

    public IReadOnlyCollection<Skill> Skills => _skills;

    public bool IsExpired => Deadline <= _clock.UtcNow;

    public event Action<Job>? Published;

    public void Publish()
    {
        if (Status != JobStatus.Draft)
        {
            throw new InvalidJobStateException("Chỉ tin tuyển dụng ở trạng thái Draft mới được đăng.");
        }

        Status = JobStatus.Published;
        Published?.Invoke(this);
    }

    public void Close()
    {
        if (Status != JobStatus.Published)
        {
            throw new InvalidJobStateException("Chỉ tin tuyển dụng ở trạng thái Published mới được đóng.");
        }

        Status = JobStatus.Closed;
    }

    public bool AddSkill(Skill skill)
    {
        ArgumentNullException.ThrowIfNull(skill);

        if (_skills.Contains(skill))
        {
            return false;
        }

        if (_skills.Count >= MaxSkillsPerJob)
        {
            throw new InvalidOperationException($"Một tin tuyển dụng không được có quá {MaxSkillsPerJob} kỹ năng.");
        }

        return _skills.Add(skill);
    }
}
