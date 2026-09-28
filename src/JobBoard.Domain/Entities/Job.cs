using JobBoard.Domain.Abstractions;
using JobBoard.Domain.Exceptions;

namespace JobBoard.Domain.Entities;

public sealed class Job
{
    public const int MaxSkillsPerJob = 15;

    private readonly IClock _clock;
    private readonly IReadOnlyList<string> _benefits;
    private readonly IReadOnlyList<string> _description;
    private readonly IReadOnlyList<string> _requirements;
    private readonly HashSet<Skill> _skills = new(SkillNameComparer.Instance);

    public Job(
        int id,
        string title,
        int companyId,
        SalaryRange salary,
        DateTime deadline,
        IClock clock,
        Company? company = null,
        string location = "",
        JobLevel level = JobLevel.Middle,
        WorkMode workMode = WorkMode.Onsite,
        string category = "",
        string summary = "",
        IEnumerable<string>? description = null,
        IEnumerable<string>? requirements = null,
        IEnumerable<string>? benefits = null,
        DateTime? postedAt = null,
        bool featured = false)
        : this(
            id,
            title,
            companyId,
            salary,
            deadline,
            clock,
            company,
            location,
            level,
            workMode,
            category,
            summary,
            description,
            requirements,
            benefits,
            postedAt,
            featured,
            JobStatus.Draft,
            validateFutureDeadline: true)
    {
    }

    private Job(
        int id,
        string title,
        int companyId,
        SalaryRange salary,
        DateTime deadline,
        IClock clock,
        Company? company,
        string location,
        JobLevel level,
        WorkMode workMode,
        string category,
        string summary,
        IEnumerable<string>? description,
        IEnumerable<string>? requirements,
        IEnumerable<string>? benefits,
        DateTime? postedAt,
        bool featured,
        JobStatus status,
        bool validateFutureDeadline)
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

        if (company is not null && company.Id != companyId)
        {
            throw new ArgumentException("Công ty không khớp với CompanyId của tin tuyển dụng.", nameof(company));
        }

        if (validateFutureDeadline && deadline <= clock.UtcNow)
        {
            throw new ArgumentException("Hạn nộp hồ sơ phải ở tương lai.", nameof(deadline));
        }

        if (!Enum.IsDefined(status))
        {
            throw new ArgumentOutOfRangeException(nameof(status), "Trạng thái tin tuyển dụng không hợp lệ.");
        }

        Id = id;
        Title = title.Trim();
        CompanyId = companyId;
        Company = company;
        Salary = salary;
        Deadline = deadline;
        Location = location.Trim();
        Level = level;
        WorkMode = workMode;
        Category = category.Trim();
        Summary = summary.Trim();
        _description = NormalizeItems(description);
        _requirements = NormalizeItems(requirements);
        _benefits = NormalizeItems(benefits);
        PostedAt = postedAt ?? clock.UtcNow;
        Featured = featured;
        Status = status;
        _clock = clock;
    }

    public int Id { get; }

    public string Title { get; }

    public int CompanyId { get; }

    public Company? Company { get; }

    public SalaryRange Salary { get; }

    public DateTime Deadline { get; }

    public string Location { get; }

    public JobLevel Level { get; }

    public WorkMode WorkMode { get; }

    public string Category { get; }

    public string Summary { get; }

    public IReadOnlyList<string> Description => _description;

    public IReadOnlyList<string> Requirements => _requirements;

    public IReadOnlyList<string> Benefits => _benefits;

    public DateTime PostedAt { get; }

    public bool Featured { get; }

    public JobStatus Status { get; private set; }

    public IReadOnlyCollection<Skill> Skills => _skills;

    public bool IsExpired => Deadline <= _clock.UtcNow;

    public event Action<Job>? Published;

    public static Job Restore(
        int id,
        string title,
        int companyId,
        SalaryRange salary,
        DateTime deadline,
        IClock clock,
        Company? company,
        string location,
        JobLevel level,
        WorkMode workMode,
        string category,
        string summary,
        IEnumerable<string>? description,
        IEnumerable<string>? requirements,
        IEnumerable<string>? benefits,
        DateTime postedAt,
        bool featured,
        JobStatus status) =>
        new(
            id,
            title,
            companyId,
            salary,
            deadline,
            clock,
            company,
            location,
            level,
            workMode,
            category,
            summary,
            description,
            requirements,
            benefits,
            postedAt,
            featured,
            status,
            validateFutureDeadline: false);

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

    private static IReadOnlyList<string> NormalizeItems(IEnumerable<string>? items) =>
        (items ?? [])
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Select(item => item.Trim())
            .ToList();
}
