namespace JobBoard.Domain.Entities;

public sealed class AdminUser(int id, string name, string email) : User(id, name, email)
{
    public override bool CanManage(Job job)
    {
        ArgumentNullException.ThrowIfNull(job);

        return true;
    }
}
