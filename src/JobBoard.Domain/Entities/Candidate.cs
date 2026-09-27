namespace JobBoard.Domain.Entities;

public sealed class Candidate : User
{
    public Candidate(int id, string name, string email, string? bio = null)
        : base(id, name, email)
    {
        Bio = bio?.Trim();
    }

    public string? Bio { get; set; }

    public override bool CanManage(Job job)
    {
        ArgumentNullException.ThrowIfNull(job);

        return false;
    }
}
