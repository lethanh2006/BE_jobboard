using JobBoard.Application.Authentication;

namespace JobBoard.Application.Abstractions.Authentication;

public interface IAccessTokenGenerator
{
    AccessToken Generate(UserAccount account);
}
