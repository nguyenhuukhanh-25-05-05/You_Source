using Microsoft.AspNetCore.Mvc;
using AppApi.DTOs;

namespace AppApi.Controllers;

public abstract class BaseController : ControllerBase
{
    protected ActionResult<ApiResponse<T>> SuccessResponse<T>(T data, string message = "Success")
    {
        return Ok(ApiResponse<T>.SuccessResponse(data, message));
    }

    protected ActionResult<ApiResponse> OkResponse(string message = "Success")
    {
        return Ok(ApiResponse.Ok(message));
    }
}
