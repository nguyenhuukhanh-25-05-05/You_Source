using AppApi.Models;

namespace AppApi.Services;

public interface ITokenService
{
    string GenerateJwtToken(ApplicationUser user, IList<string> roles);
    string GenerateRefreshToken();
    string HashToken(string token);
}