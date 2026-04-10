using MarsRoverPhotos.Models;

namespace MarsRoverPhotos.Services.Interfaces;

public interface IRoverPhotoOrchestrator
{
    Task<ProcessSummary> ProcessDatesFileAsync(string filePath, CancellationToken cancellationToken = default);
}
