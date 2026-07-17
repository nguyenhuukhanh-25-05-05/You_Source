namespace StarterAPI.Services;

public interface IFileUploadService
{
    Task<string> UploadAsync(IFormFile file, string folder = "uploads");
    Task<List<string>> UploadMultipleAsync(IEnumerable<IFormFile> files, string folder = "uploads");
    Task DeleteAsync(string filePath);
    bool IsValidFile(IFormFile file, long maxSizeBytes = 5242880, string[]? allowedExtensions = null);
}