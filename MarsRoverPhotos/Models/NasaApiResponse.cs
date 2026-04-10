using System.Text.Json.Serialization;

namespace MarsRoverPhotos.Models;

// NASA Image and Video Library API — https://images-api.nasa.gov/search
// The old api.nasa.gov/mars-photos backend was decommissioned in 2025.

public sealed class NasaImageLibraryResponse
{
    [JsonPropertyName("collection")]
    public ImageCollection Collection { get; set; } = new();
}

public sealed class ImageCollection
{
    [JsonPropertyName("items")]
    public List<ImageItem> Items { get; set; } = new();
}

public sealed class ImageItem
{
    [JsonPropertyName("data")]
    public List<ImageData> Data { get; set; } = new();

    [JsonPropertyName("links")]
    public List<ImageLink> Links { get; set; } = new();
}

public sealed class ImageData
{
    [JsonPropertyName("nasa_id")]
    public string NasaId { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("date_created")]
    public string DateCreated { get; set; } = string.Empty;
}

public sealed class ImageLink
{
    [JsonPropertyName("href")]
    public string Href { get; set; } = string.Empty;

    [JsonPropertyName("render")]
    public string Render { get; set; } = string.Empty;
}
