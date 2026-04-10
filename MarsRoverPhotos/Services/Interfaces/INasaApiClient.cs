using MarsRoverPhotos.Models;

namespace MarsRoverPhotos.Services.Interfaces;

public interface INasaApiClient
{
    Task<IReadOnlyList<RoverPhoto>> GetPhotosAsync(string earthDate, CancellationToken cancellationToken = default);
}
