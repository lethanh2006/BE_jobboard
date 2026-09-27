namespace JobBoard.Domain.Entities;

public sealed class Candidate : User
{
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

        return _skills.Add(skill);
    }

    public override bool CanManage(Job job)
    {
        ArgumentNullException.ThrowIfNull(job);

        return false;
    }
}
