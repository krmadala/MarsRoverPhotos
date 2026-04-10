using MarsRoverPhotos.Models;

namespace MarsRoverPhotos.Services.Interfaces;

public interface IDateParserService
{
    DateParseResult Parse(string input);
    IReadOnlyList<DateParseResult> ParseAll(IEnumerable<string> inputs);
}
