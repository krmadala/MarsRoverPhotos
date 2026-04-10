using MarsRoverPhotos.Models;

namespace MarsRoverPhotos.Services.Interfaces;

public interface INasaApiClient
{
    Task<IReadOnlyList<RoverPhoto>> GetPhotosAsync(string earthDate, string rover, CancellationToken cancellationToken = default);
    IReadOnlyList<MarsRover> GetAvailableRovers();
}
