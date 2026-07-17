using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using AppApi.Data;
using AppApi.DTOs;
using AppApi.Models;

namespace AppApi.Services;

public class UserService : IUserService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AppDbContext _dbContext;

    public UserService(UserManager<ApplicationUser> userManager, AppDbContext dbContext)
    {
        _userManager = userManager;
        _dbContext = dbContext;
    }

    public async Task<PagedResult<UserDto>> ListAsync(UserQuery query)
    {
        var q = _dbContext.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
            q = q.Where(u => u.UserName!.Contains(query.Search) || u.Email!.Contains(query.Search));

        if (query.IsActive.HasValue)
            q = q.Where(u => u.IsActive == query.IsActive.Value);

        var totalCount = await q.CountAsync();
        var users = await q
            .OrderBy(u => u.UserName)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        var dtos = new List<UserDto>();
        foreach (var u in users)
        {
            var roles = await _userManager.GetRolesAsync(u);
            dtos.Add(ToDto(u, roles));
        }

        return PagedResult<UserDto>.Create(dtos, totalCount, query.Page, query.PageSize);
    }

    public async Task<UserDto> CreateAsync(CreateUserRequest request)
    {
        if (await _userManager.FindByNameAsync(request.Username) != null)
            throw new InvalidOperationException("Username already exists");
        if (await _userManager.FindByEmailAsync(request.Email) != null)
            throw new InvalidOperationException("Email already exists");

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
            throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));

        await _userManager.AddToRoleAsync(user, request.Role ?? "User");
        var roles = await _userManager.GetRolesAsync(user);
        return ToDto(user, roles);
    }

    public async Task SetStatusAsync(string id, bool isActive)
    {
        var user = await _userManager.FindByIdAsync(id)
            ?? throw new KeyNotFoundException("User not found");

        if (user.IsActive == isActive)
            return;

        user.IsActive = isActive;
        await _userManager.UpdateAsync(user);

        if (!isActive)
            await _userManager.UpdateSecurityStampAsync(user);
    }

    public async Task AssignRoleAsync(string id, string role)
    {
        var user = await _userManager.FindByIdAsync(id)
            ?? throw new KeyNotFoundException("User not found");

        var current = await _userManager.GetRolesAsync(user);
        if (current.Count > 0)
            await _userManager.RemoveFromRolesAsync(user, current);

        await _userManager.AddToRoleAsync(user, role);
    }

    private static UserDto ToDto(ApplicationUser u, IList<string> roles) => new()
    {
        Id = u.Id,
        Username = u.UserName!,
        Email = u.Email!,
        FullName = u.FullName,
        IsActive = u.IsActive,
        CreatedAt = u.CreatedAt,
        LastLoginAt = u.LastLoginAt,
        Roles = roles
    };
}
