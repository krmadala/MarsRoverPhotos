# Mars Rover Photos API

<p style="color:red; font-weight:bold;">
⚠️ The original api.nasa.gov/mars-photos API was a community-built app (corincerami/mars-photo-api) hosted on Heroku. 
It was archived in October 2025 and the Heroku app is gone. 
Every request returns a Heroku "no-such-app" 404 page — it's not coming back.
</p>

An ASP.NET Core 8 Web API that reads dates from a text file, calls the
[NASA Mars Rover Photos API](https://api.nasa.gov/), and downloads rover
images locally in a structured `photos/{date}/` folder.

---

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [How to Run](#how-to-run)
   - [Visual Studio](#option-a--visual-studio-recommended)
   - [Command Line](#option-b--command-line)
3. [Configuration](#configuration)
4. [API Endpoints](#api-endpoints)
5. [Project Structure](#project-structure)
6. [Supported Date Formats](#supported-date-formats)
7. [Assumptions](#assumptions)

---

## Prerequisites

| Requirement | Version | Notes |
|---|---|---|
| [.NET 8 SDK](https://dotnet.microsoft.com/download) | 8.0+ | |
| [Visual Studio 2022](https://visualstudio.microsoft.com/) | 17.8+ | Or use the CLI |
| [NASA API Key](https://api.nasa.gov/) | — | Free registration — `DEMO_KEY` works for quick testing (rate-limited) |

---

## How to Run

### Option A — Visual Studio (Recommended)

1. **Clone the repository**
   ```
   git clone https://github.com/krmadala/MarsRoverPhotos.git
   ```

2. **Open the solution**
   Double-click `MarsRoverPhotos.sln` — Visual Studio opens with the project ready.

3. **Set your NASA API key via User Secrets** (one-time setup)

   Right-click the `MarsRoverPhotos` project → **Manage User Secrets**, then paste:
   ```json
   {
     "NasaApi": {
       "ApiKey": "YOUR_NASA_API_KEY"
     }
   }
   ```
   > User Secrets are stored outside the project folder and are never committed to git.

4. **Press F5**
   The browser opens automatically at `https://localhost:50474/swagger`.

5. **Try the API in Swagger UI**
   - Expand **GET** `/api/rover-photos/rovers` → Execute to see all available rovers.
   - Expand **POST** `/api/rover-photos/process` → click **Try it out** → **Execute**.

---

### Option B — Command Line

```bash
# 1. Clone
git clone https://github.com/krmadala/MarsRoverPhotos.git
cd MarsRoverPhotos/MarsRoverPhotos

# 2. Set API key
dotnet user-secrets set "NasaApi:ApiKey" "YOUR_NASA_API_KEY"

# 3. Run
dotnet run
```

Swagger UI → `https://localhost:50474/swagger`

---

## Configuration

All settings live in `appsettings.json`. The API key should **never** be committed —
use User Secrets locally or an environment variable in production.

```json
{
  "NasaApi": {
    "ApiKey": "YOUR_NASA_API_KEY_HERE",
    "BaseUrl": "https://api.nasa.gov/mars-photos/api/v1",
    "Rover": "curiosity",
    "MinPhotos": 3,
    "MaxPhotos": 5,
    "PhotosOutputDirectory": "photos"
  }
}
```

| Setting | Default | Description |
|---|---|---|
| `ApiKey` | — | NASA API key (use User Secrets) |
| `BaseUrl` | `https://api.nasa.gov/mars-photos/api/v1` | NASA API base URL |
| `Rover` | `curiosity` | Default rover when none is specified |
| `MinPhotos` | `3` | Minimum photos required to treat a date as "already downloaded" |
| `MaxPhotos` | `5` | Maximum photos downloaded per date |
| `PhotosOutputDirectory` | `photos` | Root folder for downloaded images |

**Override with an environment variable (no code change needed):**
```bash
# Windows
set NasaApi__ApiKey=YOUR_KEY

# Linux / macOS / Docker
export NasaApi__ApiKey=YOUR_KEY
```

---

## API Endpoints

### `GET /api/rover-photos/rovers`
Returns all five NASA Mars rovers with status and landing dates.

**Response `200 OK`:**
```json
[
  { "name": "curiosity",    "status": "active",   "landingDate": "2012-08-06", "description": "..." },
  { "name": "perseverance", "status": "active",   "landingDate": "2021-02-18", "description": "..." },
  { "name": "opportunity",  "status": "complete", "landingDate": "2004-01-25", "lastActiveDate": "2018-06-10", "description": "..." },
  { "name": "spirit",       "status": "complete", "landingDate": "2004-01-04", "lastActiveDate": "2010-03-22", "description": "..." },
  { "name": "sojourner",    "status": "complete", "landingDate": "1997-07-04", "lastActiveDate": "1997-09-27", "description": "..." }
]
```

---

### `POST /api/rover-photos/process`
Reads `dates.txt`, downloads photos for each valid date, returns a summary.

**Query parameters:**

| Parameter | Default | Description |
|---|---|---|
| `filePath` | `dates.txt` | Path to dates file (relative to app root or absolute) |
| `rover` | `curiosity` | Rover name — use `/rovers` endpoint for the full list |

**Example request:**
```
POST /api/rover-photos/process?filePath=dates.txt&rover=curiosity
```

**Response `201 Created`:**
```json
{
  "totalDatesProcessed": 5,
  "validDates": 4,
  "invalidDates": 1,
  "totalImagesDownloaded": 15,
  "totalDatesSkipped": 1,
  "results": [
    { "originalInput": "02/27/17",       "parsedDate": "2017-02-27", "isValid": true,  "imagesDownloaded": 5, "alreadyDownloaded": false },
    { "originalInput": "June 2, 2025",   "parsedDate": "2025-06-02", "isValid": true,  "imagesDownloaded": 5, "alreadyDownloaded": false },
    { "originalInput": "Jul-13-2025",    "parsedDate": "2025-07-13", "isValid": true,  "imagesDownloaded": 5, "alreadyDownloaded": false },
    { "originalInput": "April 31, 2025", "parsedDate": null,         "isValid": false, "error": "Cannot parse 'April 31, 2025'..." },
    { "originalInput": "2025-06-02",     "parsedDate": "2025-06-02", "isValid": true,  "imagesDownloaded": 5, "alreadyDownloaded": true }
  ]
}
```

**Error responses:**

| Code | Reason |
|---|---|
| `400` | File not found, empty file, invalid rover name |
| `500` | Unexpected server error (check logs) |

**Downloaded images are saved to:**
```
MarsRoverPhotos/
└── photos/
    └── 2017-02-27/
        ├── 102693.jpg
        ├── 102694.jpg
        └── 102695.jpg
```

> The `photos/` folder is excluded from git — images are regenerated on demand.

---

## Project Structure

```
MarsRoverPhotos/
├── Configuration/
│   └── NasaApiSettings.cs          # Strongly-typed config bound via IOptions<T>
├── Controllers/
│   └── RoverPhotosController.cs    # GET /rovers  |  POST /process
├── Models/
│   ├── DateParseResult.cs          # Output of date parsing
│   ├── MarsRover.cs                # Rover metadata
│   ├── NasaApiResponse.cs          # Deserialised NASA JSON
│   ├── PhotoDownloadResult.cs      # Per-date result
│   ├── ProcessSummary.cs           # Full response body
│   └── RoverPhoto.cs               # Single rover photo
├── Services/
│   ├── Interfaces/                 # Depend on these, not concretions
│   │   ├── IDateParserService.cs
│   │   ├── IFileReaderService.cs
│   │   ├── IImageDownloadService.cs
│   │   ├── INasaApiClient.cs
│   │   └── IRoverPhotoOrchestrator.cs
│   ├── DateParserService.cs        # Parses 10 date formats, rejects invalid days
│   ├── FileReaderService.cs        # Reads & trims lines from disk
│   ├── NasaApiClient.cs            # HTTP calls to NASA + rover catalogue
│   ├── ImageDownloadService.cs     # Downloads JPGs, skips existing files
│   └── RoverPhotoOrchestrator.cs   # Orchestrates all services
├── appsettings.json
├── dates.txt                       # Input — one date per line
└── Program.cs                      # DI registrations & middleware pipeline
```

---

## Supported Date Formats

| Input example | Parsed as | Notes |
|---|---|---|
| `02/27/17` | 2017-02-27 | MM/dd/yy |
| `2025-06-02` | 2025-06-02 | ISO 8601 (yyyy-MM-dd) |
| `June 2, 2025` | 2025-06-02 | Full month name |
| `Jul-13-2025` | 2025-07-13 | Abbreviated month with dashes |
| `06/02/2025` | 2025-06-02 | MM/dd/yyyy |
| `April 31, 2025` | **Invalid** | Day does not exist in April |
| *(blank line)* | **Skipped** | Empty lines are ignored |

---

## Assumptions

The following assumptions were made during design and implementation:

### Input file
- The dates file (`dates.txt`) contains **one date per line**. Lines that are blank or whitespace-only are silently skipped.
- The file is placed in the **application root** by default. An absolute path or relative sub-path can be supplied via the `?filePath=` query parameter.
- Any date that cannot be matched to a known format is treated as **invalid** and reported in the response — it does not stop the remaining dates from being processed.

### Date validation
- Date parsing uses `DateTime.TryParseExact` with a fixed set of formats and `CultureInfo.InvariantCulture`. Dates that match a format but represent a calendar day that does not exist (e.g. `April 31`) are correctly rejected.
- No future-date restriction is applied — the NASA API itself returns an empty photo list if no data exists for that date.

### Photo download
- Between **3 (min) and 5 (max)** photos are downloaded per date, controlled by `MinPhotos` / `MaxPhotos` in `appsettings.json`.
- A date is considered **already downloaded** if the `photos/{date}/` folder exists and contains at least `MinPhotos` files. In that case the NASA API is **not called** — no network request is made.
- If fewer than `MinPhotos` photos are returned by the NASA API for a given date, whatever is available is downloaded and a warning is logged — the request is not failed.
- Photos are saved as `{photoId}.jpg` inside `photos/{date}/`. Individual file download failures are logged and skipped; they do not abort the remaining photos for that date.

### Rover selection
- `curiosity` is the default rover. Any of the five known rovers (`curiosity`, `perseverance`, `opportunity`, `spirit`, `sojourner`) can be requested via the `?rover=` query parameter.
- Rover names are **case-insensitive** (`Curiosity` and `CURIOSITY` both work).
- Passing an unknown rover name returns a `400 Bad Request` with the list of valid options.

### Concurrency
- All valid dates in a single request are processed **concurrently** using `Task.WhenAll`. Result order in the response always matches the original order of lines in the input file.

### API key
- The NASA API key is read from `appsettings.json` → `NasaApi:ApiKey`. The committed file contains a placeholder (`YOUR_NASA_API_KEY_HERE`). The real key must be supplied via **.NET User Secrets** (development) or an **environment variable** (`NasaApi__ApiKey`) in production — it should never be committed to source control.

---

## Tech Stack

| Component | Technology |
|---|---|
| Framework | ASP.NET Core 8 Web API |
| JSON | `System.Text.Json` |
| HTTP | `IHttpClientFactory` typed clients |
| API docs | Swashbuckle / Swagger UI |
| Configuration | `IOptions<T>` pattern |
| Secrets | .NET User Secrets (dev) / env vars (prod) |
