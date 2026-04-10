using MarsRoverPhotos.Models;
using MarsRoverPhotos.Services.Interfaces;

namespace MarsRoverPhotos.Services;

public sealed class RoverPhotoOrchestrator : IRoverPhotoOrchestrator
{
    private readonly IFileReaderService _fileReader;
    private readonly IDateParserService _dateParser;
    private readonly INasaApiClient _nasaClient;
    private readonly IImageDownloadService _imageDownloader;
    private readonly ILogger<RoverPhotoOrchestrator> _logger;

    public RoverPhotoOrchestrator(
        IFileReaderService fileReader,
        IDateParserService dateParser,
        INasaApiClient nasaClient,
        IImageDownloadService imageDownloader,
        ILogger<RoverPhotoOrchestrator> logger)
    {
        _fileReader = fileReader;
        _dateParser = dateParser;
        _nasaClient = nasaClient;
        _imageDownloader = imageDownloader;
        _logger = logger;
    }

    public async Task<ProcessSummary> ProcessDatesFileAsync(
        string filePath,
        string rover,
        CancellationToken cancellationToken = default)
    {
        var lines = await _fileReader.ReadLinesAsync(filePath);
        var parseResults = _dateParser.ParseAll(lines);

        var summary = new ProcessSummary
        {
            TotalDatesProcessed = parseResults.Count,
            ValidDates = parseResults.Count(r => r.IsValid),
            InvalidDates = parseResults.Count(r => !r.IsValid)
        };

        // Allocate results array to preserve original input order.
        var results = new PhotoDownloadResult[parseResults.Count];

        for (var i = 0; i < parseResults.Count; i++)
        {
            if (!parseResults[i].IsValid)
            {
                results[i] = new PhotoDownloadResult
                {
                    OriginalInput = parseResults[i].OriginalValue,
                    IsValid = false,
                    Error = parseResults[i].Error
                };
            }
        }

        // Process valid dates concurrently while preserving their index.
        var validEntries = parseResults
            .Select((r, i) => (Result: r, Index: i))
            .Where(x => x.Result.IsValid)
            .ToList();

        var tasks = validEntries.Select(entry =>
            ProcessValidDateAsync(entry.Result, entry.Index, rover, results, cancellationToken));

        await Task.WhenAll(tasks);

        summary.Results = results.ToList();
        summary.TotalImagesDownloaded = results.Sum(r => r?.ImagesDownloaded ?? 0);
        summary.TotalDatesSkipped = results.Count(r => r?.AlreadyDownloaded == true);

        _logger.LogInformation(
            "Summary — rover: {Rover}, total: {Total}, valid: {Valid}, invalid: {Invalid}, images: {Images}, skipped: {Skipped}",
            rover, summary.TotalDatesProcessed, summary.ValidDates, summary.InvalidDates,
            summary.TotalImagesDownloaded, summary.TotalDatesSkipped);

        return summary;
    }

    private async Task ProcessValidDateAsync(
        DateParseResult parseResult,
        int index,
        string rover,
        PhotoDownloadResult[] results,
        CancellationToken cancellationToken)
    {
        var earthDate = parseResult.ParsedDate!.Value.ToString("yyyy-MM-dd");

        var baseResult = new PhotoDownloadResult
        {
            OriginalInput = parseResult.OriginalValue,
            ParsedDate = earthDate,
            IsValid = true
        };

        try
        {
            var photos = await _nasaClient.GetPhotosAsync(earthDate, rover, cancellationToken);

            if (photos.Count == 0)
            {
                _logger.LogWarning("No photos found for {Rover} on {Date}", rover, earthDate);
                baseResult.Error = $"No photos available for {rover} on {earthDate}.";
                results[index] = baseResult;
                return;
            }

            var downloadResult = await _imageDownloader.DownloadPhotosAsync(earthDate, photos, cancellationToken);
            downloadResult.OriginalInput = parseResult.OriginalValue;
            results[index] = downloadResult;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Error processing {Rover} on {Date}", rover, earthDate);
            baseResult.Error = ex.Message;
            results[index] = baseResult;
        }
    }
}
