namespace JobBoard.Application.Authentication;

public static class PasswordPolicy
{
    public const int MinimumLength = 10;

    public static void Validate(string password)
    {
        if (string.IsNullOrWhiteSpace(password) ||
            password.Length < MinimumLength ||
            !password.Any(char.IsUpper) ||
            !password.Any(char.IsLower) ||
            !password.Any(char.IsDigit) ||
            !password.Any(character => !char.IsLetterOrDigit(character)))
        {
            throw new ArgumentException(
                $"Mật khẩu phải có ít nhất {MinimumLength} ký tự, gồm chữ hoa, chữ thường, số và ký tự đặc biệt.",
                nameof(password));
        }
    }
}
