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
        // API key is appended at call time; never logged or exposed in URLs written to structured logs.
        var url = $"{_settings.BaseUrl}/rovers/{_settings.Rover}/photos" +
                  $"?earth_date={earthDate}&api_key={_settings.ApiKey}";

        _logger.LogInformation("Fetching Mars Rover photos for {Date}", earthDate);

        try
        {
            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var apiResponse = JsonSerializer.Deserialize<NasaApiResponse>(content, JsonOptions);

            var photos = apiResponse?.Photos ?? new List<RoverPhoto>();
            _logger.LogInformation("NASA API returned {Count} photos for {Date}", photos.Count, earthDate);
            return photos;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error calling NASA API for {Date}: {StatusCode}", earthDate, ex.StatusCode);
            throw;
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(ex, "NASA API request timed out for {Date}", earthDate);
            throw new TimeoutException($"Request to NASA API timed out for date {earthDate}.", ex);
        }
    }
}
