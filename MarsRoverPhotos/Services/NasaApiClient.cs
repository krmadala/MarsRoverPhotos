using System.Globalization;
using System.Text.Json;
using MarsRoverPhotos.Configuration;
using MarsRoverPhotos.Models;
using MarsRoverPhotos.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace MarsRoverPhotos.Services;

public sealed class NasaApiClient : INasaApiClient
{
    private readonly HttpClient _httpClient;
    private readonly NasaApiSettings _settings;
    private readonly ILogger<NasaApiClient> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public NasaApiClient(HttpClient httpClient, IOptions<NasaApiSettings> settings, ILogger<NasaApiClient> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<IReadOnlyList<RoverPhoto>> GetPhotosAsync(string earthDate, CancellationToken cancellationToken = default)
    {
        // The old api.nasa.gov/mars-photos backend was decommissioned (Heroku).
        // We now use the NASA Image and Video Library which requires no API key.
        var year = DateTime.ParseExact(earthDate, "yyyy-MM-dd", CultureInfo.InvariantCulture).Year;
        var query = Uri.EscapeDataString(_settings.SearchQuery);
        var url = $"{_settings.BaseUrl}/search?q={query}&media_type=image&year_start={year}&year_end={year}&page=1";

        _logger.LogInformation("Fetching Mars photos for {Date} (year {Year}) from NASA Image Library", earthDate, year);

        try
        {
            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var apiResponse = JsonSerializer.Deserialize<NasaImageLibraryResponse>(content, JsonOptions);

            var photos = MapToRoverPhotos(apiResponse?.Collection.Items ?? new(), earthDate);
            _logger.LogInformation("Found {Count} photos for {Date}", photos.Count, earthDate);
            return photos;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error calling NASA Image Library for {Date}: {StatusCode}", earthDate, ex.StatusCode);
            throw;
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(ex, "NASA Image Library request timed out for {Date}", earthDate);
            throw new TimeoutException($"Request to NASA Image Library timed out for date {earthDate}.", ex);
        }
    }

    private static List<RoverPhoto> MapToRoverPhotos(List<ImageItem> items, string earthDate)
    {
        var photos = new List<RoverPhoto>(items.Count);

        foreach (var item in items)
        {
            var imageUrl = item.Links.FirstOrDefault(l => l.Render == "image")?.Href;
            if (string.IsNullOrEmpty(imageUrl)) continue;

            var data = item.Data.FirstOrDefault();
            if (data is null) continue;

            photos.Add(new RoverPhoto
            {
                Id = Math.Abs(data.NasaId.GetHashCode()),
                ImgSrc = imageUrl,
                EarthDate = earthDate
            });
        }

        return photos;
    }
}
