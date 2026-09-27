namespace JobBoard.Domain.Entities;

public sealed class Candidate
{
    public Candidate(int id, string name, string email, string? bio = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Tên ứng viên không được để trống.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email ứng viên không được để trống.", nameof(email));
        }

        Id = id;
        Name = name.Trim();
        Email = email.Trim();
        Bio = bio?.Trim();
    }

    public int Id { get; }

    public string Name { get; }

    public string Email { get; }

    public string? Bio { get; set; }
}
