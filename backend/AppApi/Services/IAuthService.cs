using AppApi.DTOs;

namespace AppApi.Services;

public interface IAuthService
{
    Task<AuthResult> LoginAsync(LoginRequest request, string? deviceInfo = null);
    Task<AuthResult> RegisterAsync(RegisterRequest request, string? deviceInfo = null);
    Task<AuthResult> RefreshTokenAsync(string refreshToken, string? deviceInfo = null);
    Task RevokeAsync(string refreshToken);
}
