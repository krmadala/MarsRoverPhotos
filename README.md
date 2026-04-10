# Mars Rover Photos API

An ASP.NET Core 8 Web API that reads dates from a text file, calls the [NASA Mars Rover Photos API](https://api.nasa.gov/), and downloads Curiosity rover images locally in a structured folder layout.

---

## Features

- Parses multiple date formats (`MM/dd/yy`, `June 2 2025`, `Jul-13-2025`, `yyyy-MM-dd`, etc.)
- Validates dates — rejects non-existent dates like `April 31`
- Downloads 3–5 photos per date into `photos/{date}/`
- Skips dates that are already downloaded
- Returns a structured `201 Created` summary with per-date results
- Clean layered architecture with full Dependency Injection

---

## Project Structure

```
MarsRoverPhotos/
├── Configuration/          # Strongly-typed settings (IOptions<T>)
├── Controllers/            # RoverPhotosController
├── Models/                 # Request/response models
├── Services/
│   ├── Interfaces/         # Contracts for all services
│   ├── DateParserService   # Multi-format date parsing
│   ├── FileReaderService   # Reads dates.txt
│   ├── NasaApiClient       # Calls NASA API
│   ├── ImageDownloadService# Downloads & deduplicates images
│   └── RoverPhotoOrchestrator # Coordinates all services
├── appsettings.json
├── dates.txt               # Input file with dates
└── Program.cs
```

---

## Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- A [NASA API key](https://api.nasa.gov/) (free — `DEMO_KEY` works for testing)

### Configuration

Set your NASA API key using .NET User Secrets (recommended — never commit real keys):

```bash
cd MarsRoverPhotos
dotnet user-secrets init
dotnet user-secrets set "NasaApi:ApiKey" "YOUR_NASA_API_KEY"
```

Or set it as an environment variable:

```bash
# Linux / macOS
export NasaApi__ApiKey="YOUR_NASA_API_KEY"

# Windows
set NasaApi__ApiKey=YOUR_NASA_API_KEY
```

### Run

```bash
cd MarsRoverPhotos
dotnet run
```

Swagger UI is available at `https://localhost:{port}/swagger`.

---

## Usage

Add dates to `dates.txt` (one per line, mixed formats supported):

```
02/27/17
June 2, 2025
Jul-13-2025
April 31, 2025
2025-06-02
```

Then call the endpoint:

```bash
POST /api/rover-photos/process?filePath=dates.txt
```

### Example Response (`201 Created`)

```json
{
  "totalDatesProcessed": 5,
  "validDates": 4,
  "invalidDates": 1,
  "totalImagesDownloaded": 15,
  "totalDatesSkipped": 0,
  "results": [
    { "originalInput": "02/27/17",       "parsedDate": "2017-02-27", "isValid": true,  "imagesDownloaded": 5 },
    { "originalInput": "June 2, 2025",   "parsedDate": "2025-06-02", "isValid": true,  "imagesDownloaded": 5 },
    { "originalInput": "Jul-13-2025",    "parsedDate": "2025-07-13", "isValid": true,  "imagesDownloaded": 5 },
    { "originalInput": "April 31, 2025", "parsedDate": null,         "isValid": false, "error": "Cannot parse 'April 31, 2025'..." },
    { "originalInput": "2025-06-02",     "parsedDate": "2025-06-02", "isValid": true,  "alreadyDownloaded": true, "imagesDownloaded": 5 }
  ]
}
```

Downloaded images are saved to:

```
photos/
└── 2025-06-02/
    ├── 102693.jpg
    ├── 102694.jpg
    └── 102695.jpg
```

---

## Supported Date Formats

| Input | Parsed As |
|---|---|
| `02/27/17` | 2017-02-27 |
| `June 2, 2025` | 2025-06-02 |
| `Jul-13-2025` | 2025-07-13 |
| `2025-06-02` | 2025-06-02 |
| `April 31, 2025` | **Invalid** (day does not exist) |

---

## API Key Security

| Layer | Mechanism |
|---|---|
| Local dev | .NET User Secrets |
| CI/CD | Environment variables |
| Production | Azure Key Vault / environment variables |
| Git | `.gitignore` blocks `appsettings.Local.json`, `.env` |
| GitHub | Secret scanning + push protection enabled |

---

## Tech Stack

- ASP.NET Core 8 Web API
- `System.Text.Json` for NASA API deserialization
- `IHttpClientFactory` typed clients
- Swashbuckle / Swagger UI
- `IOptions<T>` configuration pattern
