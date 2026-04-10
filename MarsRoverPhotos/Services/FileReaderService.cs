using MarsRoverPhotos.Services.Interfaces;

namespace MarsRoverPhotos.Services;

public sealed class FileReaderService : IFileReaderService
{
    private readonly ILogger<FileReaderService> _logger;

    public FileReaderService(ILogger<FileReaderService> logger)
    {
        _logger = logger;
    }

    public async Task<IReadOnlyList<string>> ReadLinesAsync(string filePath)
    {
        if (!File.Exists(filePath))
        {
            _logger.LogError("File not found: {FilePath}", filePath);
            throw new FileNotFoundException($"Date file not found: '{filePath}'", filePath);
        }

        var lines = await File.ReadAllLinesAsync(filePath);

        var result = lines
            .Select(l => l.Trim())
            .Where(l => !string.IsNullOrEmpty(l))
            .ToList();

        _logger.LogInformation("Read {Count} non-empty lines from '{FilePath}'", result.Count, filePath);
        return result;
    }
}
