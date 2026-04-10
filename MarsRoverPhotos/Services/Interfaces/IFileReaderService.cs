namespace MarsRoverPhotos.Services.Interfaces;

public interface IFileReaderService
{
    Task<IReadOnlyList<string>> ReadLinesAsync(string filePath);
}
