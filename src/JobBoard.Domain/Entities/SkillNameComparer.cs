namespace JobBoard.Domain.Entities;

internal sealed class SkillNameComparer : IEqualityComparer<Skill>
{
    public static readonly SkillNameComparer Instance = new();

    public bool Equals(Skill? x, Skill? y) =>
        ReferenceEquals(x, y) ||
        (x is not null && y is not null && string.Equals(x.Name, y.Name, StringComparison.OrdinalIgnoreCase));

    public int GetHashCode(Skill obj) =>
        StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Name);
}
