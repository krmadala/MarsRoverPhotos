using MarsRoverPhotos.Models;
using MarsRoverPhotos.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace MarsRoverPhotos.Controllers;

[ApiController]
[Route("api/rover-photos")]
public class RoverPhotosController : ControllerBase
{
    private readonly IRoverPhotoOrchestrator _orchestrator;
    private readonly INasaApiClient _nasaClient;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<RoverPhotosController> _logger;

    public RoverPhotosController(
        IRoverPhotoOrchestrator orchestrator,
        INasaApiClient nasaClient,
        IWebHostEnvironment environment,
        ILogger<RoverPhotosController> logger)
    {
        _orchestrator = orchestrator;
        _nasaClient = nasaClient;
        _environment = environment;
        _logger = logger;
    }

    /// <summary>
    /// Returns all available NASA Mars rovers with status and landing dates.
    /// </summary>
    [HttpGet("rovers")]
    [ProducesResponseType(typeof(IReadOnlyList<MarsRover>), StatusCodes.Status200OK)]
    public IActionResult GetRovers()
    {
        var rovers = _nasaClient.GetAvailableRovers();
        return Ok(rovers);
    }

    /// <summary>
    /// Read dates from a text file, download Mars Rover photos for each valid date,
    /// and return a structured summary.
    /// </summary>
    /// <param name="filePath">Path to the dates file. Defaults to <c>dates.txt</c>.</param>
    /// <param name="rover">Rover name. Defaults to <c>curiosity</c>. Use GET /rovers for the full list.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost("process")]
    [ProducesResponseType(typeof(ProcessSummary), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ProcessDatesAsync(
        [FromQuery] string filePath = "dates.txt",
        [FromQuery] string rover = "curiosity",
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return Problem(detail: "The 'filePath' query parameter must not be empty.",
                statusCode: StatusCodes.Status400BadRequest, title: "Invalid Request");

        var validRovers = _nasaClient.GetAvailableRovers().Select(r => r.Name).ToHashSet();
        if (!validRovers.Contains(rover.ToLowerInvariant()))
        {
            return Problem(
                detail: $"Unknown rover '{rover}'. Valid options: {string.Join(", ", validRovers)}.",
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid Rover");
        }

        var resolvedPath = Path.IsPathRooted(filePath)
            ? filePath
            : Path.Combine(_environment.ContentRootPath, filePath);

        if (!System.IO.File.Exists(resolvedPath))
            return Problem(detail: $"Dates file not found: '{filePath}'.",
                statusCode: StatusCodes.Status400BadRequest, title: "File Not Found");

        try
        {
            _logger.LogInformation("Processing {FilePath} for rover '{Rover}'", resolvedPath, rover);
            var summary = await _orchestrator.ProcessDatesFileAsync(resolvedPath, rover, cancellationToken);

            if (summary.TotalDatesProcessed == 0)
                return Problem(detail: "The dates file is empty or contains no processable entries.",
                    statusCode: StatusCodes.Status400BadRequest, title: "Empty File");

            return StatusCode(StatusCodes.Status201Created, summary);
        }
        catch (OperationCanceledException)
        {
            return Problem(detail: "The request was cancelled.", statusCode: 499, title: "Request Cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error processing '{FilePath}' for rover '{Rover}'", resolvedPath, rover);
            return Problem(detail: "An unexpected error occurred. See server logs for details.",
                statusCode: StatusCodes.Status500InternalServerError, title: "Internal Server Error");
        }
    }
}
