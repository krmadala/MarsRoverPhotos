using MarsRoverPhotos.Models;
using MarsRoverPhotos.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace MarsRoverPhotos.Controllers;

[ApiController]
[Route("api/rover-photos")]
public class RoverPhotosController : ControllerBase
{
    private readonly IRoverPhotoOrchestrator _orchestrator;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<RoverPhotosController> _logger;

    public RoverPhotosController(
        IRoverPhotoOrchestrator orchestrator,
        IWebHostEnvironment environment,
        ILogger<RoverPhotosController> logger)
    {
        _orchestrator = orchestrator;
        _environment = environment;
        _logger = logger;
    }

    /// <summary>
    /// Read dates from a text file, download Mars Rover photos for each valid date,
    /// and return a structured summary.
    /// </summary>
    /// <param name="filePath">
    /// Path to the dates file. Relative paths are resolved from the application content root.
    /// Defaults to <c>dates.txt</c>.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// 201 Created with a <see cref="ProcessSummary"/> on success.
    /// 400 Bad Request if the file is missing or contains no parseable dates.
    /// </returns>
    [HttpPost("process")]
    [ProducesResponseType(typeof(ProcessSummary), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ProcessDatesAsync(
        [FromQuery] string filePath = "dates.txt",
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return Problem(
                detail: "The 'filePath' query parameter must not be empty.",
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid Request");
        }

        var resolvedPath = Path.IsPathRooted(filePath)
            ? filePath
            : Path.Combine(_environment.ContentRootPath, filePath);

        if (!System.IO.File.Exists(resolvedPath))
        {
            return Problem(
                detail: $"Dates file not found: '{filePath}'. Place the file in the application root or supply an absolute path.",
                statusCode: StatusCodes.Status400BadRequest,
                title: "File Not Found");
        }

        try
        {
            _logger.LogInformation("Processing dates file: {ResolvedPath}", resolvedPath);
            var summary = await _orchestrator.ProcessDatesFileAsync(resolvedPath, cancellationToken);

            if (summary.TotalDatesProcessed == 0)
            {
                return Problem(
                    detail: "The dates file is empty or contains no processable entries.",
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Empty File");
            }

            return StatusCode(StatusCodes.Status201Created, summary);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Request was cancelled by the client");
            return Problem(
                detail: "The request was cancelled.",
                statusCode: 499,
                title: "Request Cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error while processing dates file '{FilePath}'", resolvedPath);
            return Problem(
                detail: "An unexpected error occurred. See server logs for details.",
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Internal Server Error");
        }
    }
}
