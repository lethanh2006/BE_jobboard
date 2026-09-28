using JobBoard.Application.Authentication;

namespace JobBoard.Application.Abstractions.Authentication;

public interface IRefreshTokenGenerator
{
    IssuedRefreshToken Generate(int userId, Guid? familyId = null);

    string Hash(string token);
}
