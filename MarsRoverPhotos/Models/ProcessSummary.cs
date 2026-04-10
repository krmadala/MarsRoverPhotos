namespace MarsRoverPhotos.Models;

public sealed class ProcessSummary
{
    public int TotalDatesProcessed { get; set; }
    public int ValidDates { get; set; }
    public int InvalidDates { get; set; }
    public int TotalImagesDownloaded { get; set; }
    public int TotalDatesSkipped { get; set; }
    public List<PhotoDownloadResult> Results { get; set; } = new();
}
