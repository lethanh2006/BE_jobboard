namespace JobBoard.Domain.Entities;

public sealed class Candidate : User
{
    public const int MaxSkills = 30;

    private readonly HashSet<Skill> _skills = new(SkillNameComparer.Instance);

    public Candidate(
        int id,
        string name,
        string email,
        string? bio = null,
        SalaryRange? desiredSalary = null)
        : base(id, name, email)
    {
        Bio = bio?.Trim();
        DesiredSalary = desiredSalary;
    }

    public string? Bio { get; set; }

    public SalaryRange? DesiredSalary { get; set; }

    public IReadOnlyCollection<Skill> Skills => _skills;

    public bool AddSkill(Skill skill)
    {
        ArgumentNullException.ThrowIfNull(skill);

        if (_skills.Contains(skill))
        {
            return false;
        }

        if (_skills.Count >= MaxSkills)
        {
            throw new InvalidOperationException($"Ứng viên không được có quá {MaxSkills} kỹ năng.");
        }

        return _skills.Add(skill);
    }

    public void UpdateProfile(
        string email,
        string? bio,
        SalaryRange? desiredSalary,
        IEnumerable<Skill> skills)
    {
        ArgumentNullException.ThrowIfNull(skills);

        var normalizedSkills = new HashSet<Skill>(skills, SkillNameComparer.Instance);
        if (normalizedSkills.Count > MaxSkills)
        {
            throw new ArgumentException($"Ứng viên không được có quá {MaxSkills} kỹ năng.", nameof(skills));
        }

        ChangeEmail(email);
        Bio = bio?.Trim();
        DesiredSalary = desiredSalary;
        _skills.Clear();
        _skills.UnionWith(normalizedSkills);
    }

    public override bool CanManage(Job job)
    {
        ArgumentNullException.ThrowIfNull(job);

        return false;
    }
}
