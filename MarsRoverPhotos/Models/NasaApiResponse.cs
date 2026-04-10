using System.Text.Json.Serialization;

namespace MarsRoverPhotos.Models;

public sealed class NasaApiResponse
{
    [JsonPropertyName("photos")]
    public List<RoverPhoto> Photos { get; set; } = new();
}
