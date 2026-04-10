namespace MarsRoverPhotos.Models;

public sealed class PhotoDownloadResult
{
    public string OriginalInput { get; set; } = string.Empty;
    public string? ParsedDate { get; set; }
    public bool IsValid { get; set; }
    public bool AlreadyDownloaded { get; set; }
    public int ImagesDownloaded { get; set; }
    public List<string> DownloadedFiles { get; set; } = new();
    public string? Error { get; set; }
}
