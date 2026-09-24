using Drive.Core.Entities;

namespace Drive.Application.Authentication.Interfaces;

public interface ITokenService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
}