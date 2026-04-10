namespace MarsRoverPhotos.Models;

public sealed class MarsRover
{
    public string Name { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string LandingDate { get; init; } = string.Empty;
    public string? LastActiveDate { get; init; }
    public string Description { get; init; } = string.Empty;
}
