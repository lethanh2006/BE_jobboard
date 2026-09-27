namespace JobBoard.Domain.Entities;

public sealed class Company
{
    public Company(int id, string name, string? location = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Tên công ty không được để trống.", nameof(name));
        }

        Id = id;
        Name = name.Trim();
        Location = location?.Trim();
    }

    public int Id { get; }

    public string Name { get; }

    public string? Location { get; }
}
