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

    // All five NASA Mars rovers with key facts.
    private static readonly IReadOnlyList<MarsRover> KnownRovers = new[]
    {
        new MarsRover
        {
            Name        = "curiosity",
            Status      = "active",
            LandingDate = "2012-08-06",
            Description = "Car-sized rover still exploring Gale Crater. Longest-running Mars surface mission."
        },
        new MarsRover
        {
            Name        = "perseverance",
            Status      = "active",
            LandingDate = "2021-02-18",
            Description = "Seeking signs of ancient life in Jezero Crater. Deployed Ingenuity helicopter."
        },
        new MarsRover
        {
            Name        = "opportunity",
            Status      = "complete",
            LandingDate = "2004-01-25",
            LastActiveDate = "2018-06-10",
            Description = "Operated for 15 years — 45x its planned 90-day mission. Explored Meridiani Planum."
        },
        new MarsRover
        {
            Name        = "spirit",
            Status      = "complete",
            LandingDate = "2004-01-04",
            LastActiveDate = "2010-03-22",
            Description = "Twin of Opportunity. Explored Gusev Crater for over 6 years."
        },
        new MarsRover
        {
            Name        = "sojourner",
            Status      = "complete",
            LandingDate = "1997-07-04",
            LastActiveDate = "1997-09-27",
            Description = "First Mars rover. Part of the Mars Pathfinder mission. Operated for 83 days."
        }
    };

    public NasaApiClient(HttpClient httpClient, IOptions<NasaApiSettings> settings, ILogger<NasaApiClient> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    public IReadOnlyList<MarsRover> GetAvailableRovers() => KnownRovers;

    public async Task<IReadOnlyList<RoverPhoto>> GetPhotosAsync(
        string earthDate,
        string rover,
        CancellationToken cancellationToken = default)
    {
        var roverName = rover.ToLowerInvariant();
        var url = $"{_settings.BaseUrl}/rovers/{roverName}/photos" +
                  $"?earth_date={earthDate}&api_key={_settings.ApiKey}";

        _logger.LogInformation("Fetching {Rover} photos for {Date}", roverName, earthDate);

        try
        {
            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var apiResponse = JsonSerializer.Deserialize<NasaApiResponse>(content, JsonOptions);

            var photos = apiResponse?.Photos ?? new List<RoverPhoto>();
            _logger.LogInformation("NASA API returned {Count} photos for {Rover} on {Date}", photos.Count, roverName, earthDate);
            return photos;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error calling NASA API for {Rover} on {Date}: {StatusCode}", roverName, earthDate, ex.StatusCode);
            throw;
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(ex, "NASA API request timed out for {Rover} on {Date}", roverName, earthDate);
            throw new TimeoutException($"Request to NASA API timed out for {roverName} on {earthDate}.", ex);
        }
    }
}
