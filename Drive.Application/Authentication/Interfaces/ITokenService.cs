using Drive.Core.Entities;

namespace Drive.Application.Authentication.Interfaces;

public interface ITokenService
{
    string GenerateAccessToken(User user);
    public string GenerateRefreshToken();
    public string HashRefreshToken(string token);
}