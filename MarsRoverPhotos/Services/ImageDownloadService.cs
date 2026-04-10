using MarsRoverPhotos.Configuration;
using MarsRoverPhotos.Models;
using MarsRoverPhotos.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace MarsRoverPhotos.Services;

public sealed class ImageDownloadService : IImageDownloadService
{
    private readonly HttpClient _httpClient;
    private readonly NasaApiSettings _settings;
    private readonly ILogger<ImageDownloadService> _logger;

    public ImageDownloadService(HttpClient httpClient, IOptions<NasaApiSettings> settings, ILogger<ImageDownloadService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    public bool IsAlreadyDownloaded(string earthDate, out int existingCount)
    {
        var folder = GetDateFolder(earthDate);
        if (Directory.Exists(folder))
        {
            existingCount = Directory.GetFiles(folder, "*.jpg").Length;
            return existingCount >= _settings.MinPhotos;
        }

        existingCount = 0;
        return false;
    }

    public async Task<PhotoDownloadResult> DownloadPhotosAsync(
        string earthDate,
        IReadOnlyList<RoverPhoto> photos,
        CancellationToken cancellationToken = default)
    {
        if (IsAlreadyDownloaded(earthDate, out var existingCount))
        {
            _logger.LogInformation(
                "Photos for {Date} already downloaded ({Count} files found, minimum is {Min}). Skipping.",
                earthDate, existingCount, _settings.MinPhotos);

            return new PhotoDownloadResult
            {
                ParsedDate = earthDate,
                IsValid = true,
                AlreadyDownloaded = true,
                ImagesDownloaded = existingCount
            };
        }

        if (photos.Count < _settings.MinPhotos)
        {
            _logger.LogWarning(
                "Only {Available} photos available for {Date}; minimum required is {Min}. Downloading what is available.",
                photos.Count, earthDate, _settings.MinPhotos);
        }

        var folder = GetDateFolder(earthDate);
        Directory.CreateDirectory(folder);

        var toDownload = photos.Take(_settings.MaxPhotos).ToList();
        var downloadedFiles = new List<string>(toDownload.Count);

        foreach (var photo in toDownload)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var fileName = Path.Combine(folder, $"{photo.Id}.jpg");

            if (File.Exists(fileName))
            {
                _logger.LogDebug("Photo {Id} already on disk, skipping", photo.Id);
                downloadedFiles.Add(fileName);
                continue;
            }

            try
            {
                _logger.LogDebug("Downloading photo {Id} for {Date} from {Url}", photo.Id, earthDate, photo.ImgSrc);
                var bytes = await _httpClient.GetByteArrayAsync(photo.ImgSrc, cancellationToken);
                await File.WriteAllBytesAsync(fileName, bytes, cancellationToken);
                downloadedFiles.Add(fileName);
                _logger.LogDebug("Saved {FileName}", fileName);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Failed to download photo {Id} for {Date}", photo.Id, earthDate);
            }
        }

        return new PhotoDownloadResult
        {
            ParsedDate = earthDate,
            IsValid = true,
            AlreadyDownloaded = false,
            ImagesDownloaded = downloadedFiles.Count,
            DownloadedFiles = downloadedFiles
        };
    }

    private string GetDateFolder(string earthDate)
        => Path.Combine(_settings.PhotosOutputDirectory, earthDate);
}
