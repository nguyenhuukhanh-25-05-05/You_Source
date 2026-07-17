using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AppApi.DTOs;
using AppApi.Services;

namespace AppApi.Controllers;

[Authorize(Roles = "Admin")]
[Route("api/[controller]")]
[ApiController]
public class UsersController : BaseController
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<UserDto>>>> List([FromQuery] UserQuery query)
    {
        var result = await _userService.ListAsync(query);
        return SuccessResponse(result);
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<UserDto>>> Create([FromBody] CreateUserRequest request)
    {
        var user = await _userService.CreateAsync(request);
        return SuccessResponse(user, "User created");
    }

    [HttpPatch("{id}/status")]
    public async Task<ActionResult<ApiResponse>> SetStatus(string id, [FromBody] UpdateUserStatusRequest request)
    {
        await _userService.SetStatusAsync(id, request.IsActive);
        return OkResponse("User status updated");
    }

    [HttpPost("{id}/roles")]
    public async Task<ActionResult<ApiResponse>> AssignRole(string id, [FromBody] AssignRoleRequest request)
    {
        await _userService.AssignRoleAsync(id, request.Role);
        return OkResponse("Role assigned");
    }
}
