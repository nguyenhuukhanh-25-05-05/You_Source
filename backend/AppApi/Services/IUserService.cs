using AppApi.DTOs;
using AppApi.Models;

namespace AppApi.Services;

public interface IUserService
{
    Task<PagedResult<UserDto>> ListAsync(UserQuery query);
    Task<UserDto> CreateAsync(CreateUserRequest request);
    Task SetStatusAsync(string id, bool isActive);
    Task AssignRoleAsync(string id, string role);
}
