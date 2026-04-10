namespace MarsRoverPhotos.Configuration;

public sealed class NasaApiSettings
{
    public string BaseUrl { get; set; } = "https://images-api.nasa.gov";

    // Free-text query sent to the NASA Image Library search endpoint.
    public string SearchQuery { get; set; } = "mars curiosity rover";

    public int MinPhotos { get; set; } = 3;
    public int MaxPhotos { get; set; } = 5;
    public string PhotosOutputDirectory { get; set; } = "photos";
}
