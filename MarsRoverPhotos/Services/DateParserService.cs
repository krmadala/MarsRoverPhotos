using System.Globalization;
using MarsRoverPhotos.Models;
using MarsRoverPhotos.Services.Interfaces;

namespace MarsRoverPhotos.Services;

public sealed class DateParserService : IDateParserService
{
    // Ordered from most specific to least specific to avoid ambiguity.
    private static readonly string[] SupportedFormats =
    {
        "yyyy-MM-dd",       // 2018-06-02
        "MM/dd/yyyy",       // 06/02/2018
        "MM/dd/yy",         // 02/27/17
        "M/d/yyyy",         // 6/2/2018
        "M/d/yy",           // 6/2/17
        "MMMM d, yyyy",     // June 2, 2018
        "MMMM dd, yyyy",    // June 02, 2018
        "MMM-dd-yyyy",      // Jul-13-2016
        "MMM-d-yyyy",       // Jul-3-2016
        "MM-dd-yyyy",       // 06-02-2018
    };

    private readonly ILogger<DateParserService> _logger;

    public DateParserService(ILogger<DateParserService> logger)
    {
        _logger = logger;
    }

    public DateParseResult Parse(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return new DateParseResult
            {
                OriginalValue = input ?? string.Empty,
                IsValid = false,
                Error = "Date input is empty."
            };
        }

        var trimmed = input.Trim();

        if (DateTime.TryParseExact(trimmed, SupportedFormats, CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var parsed))
        {
            _logger.LogDebug("Parsed '{Input}' -> {Date:yyyy-MM-dd}", trimmed, parsed);
            return new DateParseResult
            {
                OriginalValue = trimmed,
                ParsedDate = parsed,
                IsValid = true
            };
        }

        _logger.LogWarning("Failed to parse date: '{Input}'", trimmed);
        return new DateParseResult
        {
            OriginalValue = trimmed,
            IsValid = false,
            Error = $"Cannot parse '{trimmed}'. Ensure the date is real and uses a supported format: " +
                    "MM/dd/yy, MM/dd/yyyy, MMMM d yyyy, MMM-dd-yyyy, yyyy-MM-dd"
        };
    }

    public IReadOnlyList<DateParseResult> ParseAll(IEnumerable<string> inputs)
        => inputs.Select(Parse).ToList();
}
