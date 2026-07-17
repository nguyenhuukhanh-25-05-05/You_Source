using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;
using AppApi.DTOs;
using AppApi.Services;

namespace AppApi.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class UploadController : BaseController
{
    private readonly IFileUploadService _fileUploadService;
    private static readonly Regex SafeFolderRegex = new("^[a-zA-Z0-9_-]+$$", RegexOptions.Compiled);

    public UploadController(IFileUploadService fileUploadService)
    {
        _fileUploadService = fileUploadService;
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<string>>> Upload(IFormFile file, [FromQuery] string folder = "uploads")
    {
        if (!IsValidFolder(folder))
            return BadRequest(ApiResponse<string>.FailResponse("Invalid folder name."));

        if (!_fileUploadService.IsValidFile(file))
            return BadRequest(ApiResponse<string>.FailResponse("Invalid file. Max size: 5MB. Allowed: jpg, jpeg, png, gif, webp, pdf, doc, docx, xls, xlsx"));

        var path = await _fileUploadService.UploadAsync(file, folder);
        return SuccessResponse(path, "File uploaded successfully");
    }

    [HttpPost("multiple")]
    public async Task<ActionResult<ApiResponse<List<string>>>> UploadMultiple(
        List<IFormFile> files,
        [FromQuery] string folder = "uploads")
    {
        if (!IsValidFolder(folder))
            return BadRequest(ApiResponse<List<string>>.FailResponse("Invalid folder name."));

        foreach (var file in files)
        {
            if (!_fileUploadService.IsValidFile(file))
                return BadRequest(ApiResponse<List<string>>.FailResponse($"Invalid file: {file.FileName}"));
        }

        var paths = await _fileUploadService.UploadMultipleAsync(files, folder);
        return SuccessResponse(paths, "Files uploaded successfully");
    }

    [HttpDelete]
    public async Task<ActionResult<ApiResponse>> Delete([FromQuery] string path)
    {
        await _fileUploadService.DeleteAsync(path);
        return OkResponse("File deleted successfully");
    }

    private static bool IsValidFolder(string folder)
    {
        if (string.IsNullOrWhiteSpace(folder)) return false;
        if (folder.Length > 50) return false;
        return SafeFolderRegex.IsMatch(folder);
    }
}