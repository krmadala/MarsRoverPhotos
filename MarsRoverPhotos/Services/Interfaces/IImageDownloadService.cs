using MarsRoverPhotos.Models;

namespace MarsRoverPhotos.Services.Interfaces;

public interface IImageDownloadService
{
    bool IsAlreadyDownloaded(string earthDate, out int existingCount);
    Task<PhotoDownloadResult> DownloadPhotosAsync(string earthDate, IReadOnlyList<RoverPhoto> photos, CancellationToken cancellationToken = default);
}
