using System.Text.Json.Serialization;

namespace MarsRoverPhotos.Models;

public sealed class RoverPhoto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("img_src")]
    public string ImgSrc { get; set; } = string.Empty;

    [JsonPropertyName("earth_date")]
    public string EarthDate { get; set; } = string.Empty;

    [JsonPropertyName("camera")]
    public RoverCamera? Camera { get; set; }
}

public sealed class RoverCamera
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("full_name")]
    public string FullName { get; set; } = string.Empty;
}
