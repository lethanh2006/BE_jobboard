namespace JobBoard.Domain.Entities;

public sealed record Skill
{
    public Skill(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Tên kỹ năng không được để trống.", nameof(name));
        }

        Name = name.Trim();
    }

    public string Name { get; }
}
