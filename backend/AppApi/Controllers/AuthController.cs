using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using AppApi.DTOs;
using AppApi.Helpers;
using AppApi.Services;

namespace AppApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController : BaseController
{
    private readonly IAuthService _authService;
    private readonly IWebHostEnvironment _env;

    public AuthController(IAuthService authService, IWebHostEnvironment env)
    {
        _authService = authService;
        _env = env;
    }

    [EnableRateLimiting("auth")]
    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Login([FromBody] LoginRequest request)
    {
        var result = await _authService.LoginAsync(request, GetDeviceInfo());
        SetAuthCookies(result);
        return SuccessResponse(ToAuthResponse(result), "Login successful");
    }

    [EnableRateLimiting("auth")]
    [HttpPost("register")]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Register([FromBody] RegisterRequest request)
    {
        var result = await _authService.RegisterAsync(request, GetDeviceInfo());
        SetAuthCookies(result);
        return SuccessResponse(ToAuthResponse(result), "Registration successful");
    }

    [EnableRateLimiting("auth")]
    [HttpPost("refresh-token")]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> RefreshToken([FromBody] RefreshTokenRequest? request)
    {
        var refresh = request?.RefreshToken
            ?? Request.Cookies[AuthCookieHelper.RefreshTokenCookie]
            ?? string.Empty;
        var result = await _authService.RefreshTokenAsync(refresh, GetDeviceInfo());
        SetAuthCookies(result);
        return SuccessResponse(ToAuthResponse(result), "Token refreshed");
    }

    [Authorize]
    [HttpPost("revoke")]
    public async Task<ActionResult<ApiResponse>> Revoke()
    {
        var refresh = Request.Cookies[AuthCookieHelper.RefreshTokenCookie];
        if (!string.IsNullOrEmpty(refresh))
            await _authService.RevokeAsync(refresh);
        AuthCookieHelper.ClearAuthCookies(Response, _env.IsProduction());
        return OkResponse("Token revoked");
    }

    [Authorize]
    [HttpGet("me")]
    public ActionResult<ApiResponse<UserInfo>> Me()
    {
        var info = new UserInfo
        {
            Username = User.Identity?.Name ?? string.Empty,
            FullName = User.FindFirst("FullName")?.Value ?? string.Empty,
            Roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList()
        };
        return SuccessResponse(info, "Current user");
    }

    [EnableRateLimiting("auth")]
    [HttpPost("forgot-password")]
    public async Task<ActionResult<ApiResponse>> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        await _authService.ForgotPasswordAsync(request.Email);
        return OkResponse("If the email exists, a reset link has been sent");
    }

    [EnableRateLimiting("auth")]
    [HttpPost("reset-password")]
    public async Task<ActionResult<ApiResponse>> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        await _authService.ResetPasswordAsync(request.Email, request.Token, request.NewPassword);
        return OkResponse("Password has been reset");
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<ActionResult<ApiResponse>> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var username = User.Identity?.Name
            ?? throw new UnauthorizedAccessException("Invalid credentials");
        await _authService.ChangePasswordAsync(username, request.CurrentPassword, request.NewPassword);
        AuthCookieHelper.ClearAuthCookies(Response, _env.IsProduction());
        return OkResponse("Password changed. Please login again");
    }

    private void SetAuthCookies(AuthResult r)
    {
        AuthCookieHelper.SetAuthCookies(
            Response, r.Token, r.RefreshToken, r.ExpiresAt, r.RefreshExpiresAt, _env.IsProduction());
    }

    private string GetDeviceInfo()
    {
        var ua = Request.Headers.UserAgent.ToString();
        return ua.Length > 300 ? ua[..300] : ua;
    }

    private static AuthResponse ToAuthResponse(AuthResult r) => new()
    {
        ExpiresAt = r.ExpiresAt,
        Username = r.Username,
        FullName = r.FullName,
        Roles = r.Roles
    };
}
