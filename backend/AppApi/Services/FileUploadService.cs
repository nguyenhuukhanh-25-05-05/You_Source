using Microsoft.EntityFrameworkCore;
using AppApi.Data;
using AppApi.Models;

namespace AppApi.Services;

public class FileUploadService : IFileUploadService
{
    private readonly IWebHostEnvironment _env;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly AppDbContext _dbContext;
    private static readonly string[] DefaultAllowedExtensions =
        { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".pdf", ".doc", ".docx", ".xls", ".xlsx" };

    public FileUploadService(
        IWebHostEnvironment env,
        IHttpContextAccessor httpContextAccessor,
        AppDbContext dbContext)
    {
        _env = env;
        _httpContextAccessor = httpContextAccessor;
        _dbContext = dbContext;
    }

    public async Task<string> UploadAsync(IFormFile file, string folder = "uploads")
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("File is empty");

        if (!IsValidFile(file))
            throw new ArgumentException("Invalid file. Max size: 5MB. Allowed: jpg, jpeg, png, gif, webp, pdf, doc, docx, xls, xlsx");

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var uploadPath = ResolveSafePath(folder);

        Directory.CreateDirectory(uploadPath);

        var fileName = $"{Guid.NewGuid():N}{ext}";
        var filePath = Path.Combine(uploadPath, fileName);

        await using (var stream = new FileStream(filePath, FileMode.CreateNew))
        {
            await file.CopyToAsync(stream);
        }

        var relativePath = $"/{folder}/{fileName}";
        var user = _httpContextAccessor.HttpContext?.User;
        var userId = user?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? string.Empty;

        var record = new FileUpload
        {
            FilePath = relativePath,
            OriginalFileName = Path.GetFileName(file.FileName),
            ContentType = file.ContentType,
            Size = file.Length,
            UserId = userId
        };

        _dbContext.FileUploads.Add(record);
        await _dbContext.SaveChangesAsync();

        return relativePath;
    }

    public async Task<List<string>> UploadMultipleAsync(IEnumerable<IFormFile> files, string folder = "uploads")
    {
        var results = new List<string>();
        foreach (var file in files)
        {
            var path = await UploadAsync(file, folder);
            results.Add(path);
        }
        return results;
    }

    public async Task DeleteAsync(string filePath)
    {
        var record = await _dbContext.FileUploads
            .FirstOrDefaultAsync(f => f.FilePath == filePath);

        if (record == null)
            throw new KeyNotFoundException("File not found");

        var user = _httpContextAccessor.HttpContext?.User;
        var userId = user?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var isAdmin = user?.IsInRole("Admin") == true;

        if (!isAdmin && (userId == null || record.UserId != userId))
            throw new UnauthorizedAccessException("You are not allowed to delete this file");

        var fullPath = ResolveSafePath(filePath);
        if (File.Exists(fullPath))
            File.Delete(fullPath);

        _dbContext.FileUploads.Remove(record);
        await _dbContext.SaveChangesAsync();
    }

    public bool IsValidFile(IFormFile file, long maxSizeBytes = 5242880, string[]? allowedExtensions = null)
    {
        if (file == null || file.Length == 0)
            return false;

        if (file.Length > maxSizeBytes)
            return false;

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var allowed = allowedExtensions ?? DefaultAllowedExtensions;

        if (!allowed.Contains(ext))
            return false;

        if (!HasValidSignature(file, ext))
            return false;

        return true;
    }

    private static bool HasValidSignature(IFormFile file, string ext)
    {
        var signatures = FileSignatures.GetValueOrDefault(ext);
        if (signatures == null || signatures.Length == 0)
            return true;

        using var stream = file.OpenReadStream();
        Span<byte> header = stackalloc byte[16];
        var read = stream.Read(header);
        if (read == 0) return false;

        foreach (var sig in signatures)
        {
            if (sig.SequenceEqual(header.Slice(0, sig.Length).ToArray()))
                return true;
        }

        if (ext == ".webp" && read >= 12
            && header[0] == 0x52 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x46
            && header[8] == 0x57 && header[9] == 0x45 && header[10] == 0x42 && header[11] == 0x50)
            return true;

        return false;
    }

    private static readonly Dictionary<string, byte[][]> FileSignatures = new()
    {
        [".jpg"] = new[] { new byte[] { 0xFF, 0xD8, 0xFF } },
        [".jpeg"] = new[] { new byte[] { 0xFF, 0xD8, 0xFF } },
        [".png"] = new[] { new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A } },
        [".gif"] = new[] { new byte[] { 0x47, 0x49, 0x46, 0x38, 0x37, 0x61 }, new byte[] { 0x47, 0x49, 0x46, 0x38, 0x39, 0x61 } },
        [".webp"] = new[] { new byte[] { 0x52, 0x49, 0x46, 0x46 } },
        [".pdf"] = new[] { new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D } },
        [".doc"] = new[] { new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 } },
        [".docx"] = new[] { new byte[] { 0x50, 0x4B, 0x03, 0x04 }, new byte[] { 0x50, 0x4B, 0x05, 0x06 }, new byte[] { 0x50, 0x4B, 0x07, 0x08 } },
        [".xls"] = new[] { new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 } },
        [".xlsx"] = new[] { new byte[] { 0x50, 0x4B, 0x03, 0x04 }, new byte[] { 0x50, 0x4B, 0x05, 0x06 }, new byte[] { 0x50, 0x4B, 0x07, 0x08 } }
    };

    private string ResolveSafePath(string relativePath)
    {
        var webRoot = _env.WebRootPath
            ?? throw new InvalidOperationException("Web root path is not configured.");

        relativePath = relativePath
            .Replace('\\', '/')
            .TrimStart('/');

        var combined = Path.GetFullPath(Path.Combine(webRoot, relativePath));
        var root = Path.GetFullPath(webRoot) + Path.DirectorySeparatorChar;

        if (!combined.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException("Path traversal detected.");

        return combined;
    }
}