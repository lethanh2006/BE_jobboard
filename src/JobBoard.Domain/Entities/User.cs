namespace JobBoard.Domain.Entities;

public abstract class User
{
    protected User(int id, string name, string email)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Tên người dùng không được để trống.", nameof(name));
        }

        Id = id;
        Name = name.Trim();
        Email = NormalizeEmail(email);
    }

    public int Id { get; }

    public string Name { get; }

    public string Email { get; private set; }

    public void ChangeEmail(string email)
    {
        Email = NormalizeEmail(email);
    }

    public abstract bool CanManage(Job job);

    public virtual string GetDisplayName() => Name;

    private static string NormalizeEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email người dùng không được để trống.", nameof(email));
        }

        var normalizedEmail = email.Trim();

        if (!normalizedEmail.Contains('@'))
        {
            throw new ArgumentException("Email người dùng phải chứa ký tự @.", nameof(email));
        }

        return normalizedEmail;
    }
}
