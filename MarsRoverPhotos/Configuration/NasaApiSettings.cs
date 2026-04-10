namespace MarsRoverPhotos.Configuration;

public sealed class NasaApiSettings
{
    public string ApiKey { get; set; } = "DEMO_KEY";
    public string BaseUrl { get; set; } = "https://api.nasa.gov/mars-photos/api/v1";
    public string Rover { get; set; } = "curiosity";
    public int MinPhotos { get; set; } = 3;
    public int MaxPhotos { get; set; } = 5;
    public string PhotosOutputDirectory { get; set; } = "photos";
}
