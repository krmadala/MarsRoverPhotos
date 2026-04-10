namespace MarsRoverPhotos.Models;

public sealed class DateParseResult
{
    public string OriginalValue { get; init; } = string.Empty;
    public DateTime? ParsedDate { get; init; }
    public bool IsValid { get; init; }
    public string? Error { get; init; }
}
