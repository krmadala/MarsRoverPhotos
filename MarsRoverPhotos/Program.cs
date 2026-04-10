using MarsRoverPhotos.Configuration;
using MarsRoverPhotos.Services;
using MarsRoverPhotos.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// ── Configuration ───────────────────────────────────────────────────────────
builder.Services.Configure<NasaApiSettings>(
    builder.Configuration.GetSection("NasaApi"));

// ── HTTP Clients ─────────────────────────────────────────────────────────────
// Typed client for NASA API calls — short timeout, metadata only.
builder.Services.AddHttpClient<INasaApiClient, NasaApiClient>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Typed client for image downloads — longer timeout for binary payloads.
builder.Services.AddHttpClient<IImageDownloadService, ImageDownloadService>(client =>
{
    client.Timeout = TimeSpan.FromMinutes(2);
});

// ── Application Services ─────────────────────────────────────────────────────
builder.Services.AddScoped<IDateParserService, DateParserService>();
builder.Services.AddScoped<IFileReaderService, FileReaderService>();
builder.Services.AddScoped<IRoverPhotoOrchestrator, RoverPhotoOrchestrator>();

// ── API Infrastructure ───────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "Mars Rover Photos API",
        Version = "v1",
        Description = "Downloads NASA Mars Rover photos for a list of dates supplied in a text file."
    });
});

// ── Build & Middleware Pipeline ───────────────────────────────────────────────
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
