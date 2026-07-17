namespace AppApi.Models;

public class FileUpload : BaseEntity
{
    public string FilePath { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string? ContentType { get; set; }
    public long Size { get; set; }
    public string UserId { get; set; } = string.Empty;
}
