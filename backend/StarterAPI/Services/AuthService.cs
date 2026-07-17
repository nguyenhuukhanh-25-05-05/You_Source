using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StarterAPI.Data;
using StarterAPI.DTOs;
using StarterAPI.Models;

namespace StarterAPI.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITokenService _tokenService;
    private readonly IConfiguration _configuration;
    private readonly AppDbContext _dbContext;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        ITokenService tokenService,
        IConfiguration configuration,
        AppDbContext dbContext)
    {
        _userManager = userManager;
        _tokenService = tokenService;
        _configuration = configuration;
        _dbContext = dbContext;
    }

    public async Task<AuthResult> LoginAsync(LoginRequest request, string? deviceInfo = null)
    {
        var user = await _userManager.FindByNameAsync(request.Username);

        if (user == null)
        {
            await DoConstantTimeFakePasswordCheckAsync();
            throw new UnauthorizedAccessException("Invalid username or password");
        }

        if (await _userManager.IsLockedOutAsync(user))
        {
            await DoConstantTimeFakePasswordCheckAsync();
            throw new UnauthorizedAccessException("Invalid username or password");
        }

        if (!user.IsActive)
        {
            await DoConstantTimeFakePasswordCheckAsync();
            throw new UnauthorizedAccessException("Invalid username or password");
        }

        var passwordOk = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!passwordOk)
        {
            await _userManager.AccessFailedAsync(user);
            throw new UnauthorizedAccessException("Invalid username or password");
        }

        await _userManager.ResetAccessFailedCountAsync(user);

        user.LastLoginAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        return await IssueTokensAsync(user, deviceInfo);
    }

    private static readonly string FakePasswordHash = new PasswordHasher<ApplicationUser>()
        .HashPassword(new ApplicationUser { UserName = "_", Email = "_" }, "x");

    private static async Task DoConstantTimeFakePasswordCheckAsync()
    {
        var fake = new ApplicationUser { UserName = "_", Email = "_" };
        var dummy = new PasswordHasher<ApplicationUser>();
        dummy.VerifyHashedPassword(fake, FakePasswordHash, "x");
        await Task.Delay(Random.Shared.Next(50, 150));
    }

    public async Task<AuthResult> RegisterAsync(RegisterRequest request, string? deviceInfo = null)
    {
        var existingUser = await _userManager.FindByNameAsync(request.Username);
        if (existingUser != null)
            throw new InvalidOperationException("Registration failed");

        var existingEmail = await _userManager.FindByEmailAsync(request.Email);
        if (existingEmail != null)
            throw new InvalidOperationException("Registration failed");

        var user = new ApplicationUser
        {
            UserName = request.Username,
            Email = request.Email,
            FullName = request.FullName,
            EmailConfirmed = true,
            IsActive = true
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            throw new InvalidOperationException("Registration failed");

        await _userManager.AddToRoleAsync(user, "User");

        return await IssueTokensAsync(user, deviceInfo);
    }

    private async Task<AuthResult> IssueTokensAsync(ApplicationUser user, string? deviceInfo)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var jwt = _tokenService.GenerateJwtToken(user, roles);
        var refresh = _tokenService.GenerateRefreshToken();
        var refreshHash = _tokenService.HashToken(refresh);

        var refreshDays = double.Parse(_configuration["JwtSettings:RefreshTokenExpirationInDays"]!);
        var accessMinutes = double.Parse(_configuration["JwtSettings:ExpirationInMinutes"]!);
        var now = DateTime.UtcNow;

        _dbContext.RefreshTokens.Add(new RefreshToken
        {
            TokenHash = refreshHash,
            UserId = user.Id,
            CreatedAt = now,
            ExpiresAt = now.AddDays(refreshDays),
            DeviceInfo = deviceInfo
        });
        await _dbContext.SaveChangesAsync();

        return new AuthResult(
            jwt,
            refresh,
            now.AddMinutes(accessMinutes),
            now.AddDays(refreshDays),
            user.UserName!,
            user.FullName,
            roles);
    }

    public async Task<AuthResult> RefreshTokenAsync(string refreshToken, string? deviceInfo = null)
    {
        if (string.IsNullOrEmpty(refreshToken))
            throw new UnauthorizedAccessException("Invalid or expired refresh token");

        var hash = _tokenService.HashToken(refreshToken);
        var stored = await _dbContext.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash);

        if (stored == null)
            throw new UnauthorizedAccessException("Invalid or expired refresh token");

        var user = await _userManager.FindByIdAsync(stored.UserId);
        if (user == null || !user.IsActive)
            throw new UnauthorizedAccessException("Invalid or expired refresh token");

        if (stored.RevokedAt != null)
        {
            await RevokeAllTokensAsync(user.Id);
            _userManager.Logger.LogWarning(
                "Refresh token reuse detected for user {Username}. All sessions revoked.", user.UserName);
            throw new UnauthorizedAccessException("Invalid or expired refresh token");
        }

        if (stored.IsExpired)
        {
            stored.RevokedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();
            throw new UnauthorizedAccessException("Invalid or expired refresh token");
        }

        stored.RevokedAt = DateTime.UtcNow;

        var roles = await _userManager.GetRolesAsync(user);
        var newJwt = _tokenService.GenerateJwtToken(user, roles);
        var newRefresh = _tokenService.GenerateRefreshToken();
        var newHash = _tokenService.HashToken(newRefresh);

        var refreshDays = double.Parse(_configuration["JwtSettings:RefreshTokenExpirationInDays"]!);
        var accessMinutes = double.Parse(_configuration["JwtSettings:ExpirationInMinutes"]!);
        var now = DateTime.UtcNow;

        _dbContext.RefreshTokens.Add(new RefreshToken
        {
            TokenHash = newHash,
            UserId = user.Id,
            CreatedAt = now,
            ExpiresAt = now.AddDays(refreshDays),
            ReplacedByTokenHash = hash,
            DeviceInfo = deviceInfo
        });
        await _dbContext.SaveChangesAsync();

        return new AuthResult(
            newJwt,
            newRefresh,
            now.AddMinutes(accessMinutes),
            now.AddDays(refreshDays),
            user.UserName!,
            user.FullName,
            roles);
    }

    public async Task RevokeAsync(string refreshToken)
    {
        if (string.IsNullOrEmpty(refreshToken))
            return;

        var hash = _tokenService.HashToken(refreshToken);
        var stored = await _dbContext.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash);
        if (stored == null || stored.RevokedAt != null)
            return;

        stored.RevokedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();
    }

    private async Task RevokeAllTokensAsync(string userId)
    {
        var active = await _dbContext.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ToListAsync();
        foreach (var t in active)
            t.RevokedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();
    }
}
