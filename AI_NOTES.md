# AI Notes

---

## 1. AI Tool Used

| Tool | Model | Interface |
|---|---|---|
| **Claude** | Claude Sonnet (Anthropic) | Claude Code (CLI) |

All code generation, debugging, git operations, and documentation in this
project were produced through an interactive Claude Code session.

---

## 2. Prompts Used

The following prompts were submitted during the session, in order:

1. > *"Act as a senior .NET architect and help me design and implement a clean,
   > production-quality solution. GOAL: Build a .net web api application that
   > integrates with NASA Mars Rover Photos API to: Read dates from a text file,
   > Parse multiple date formats, Validate dates, Call API for valid dates,
   > Download rover images (min 3 max 5) if already not downloaded, Store locally
   > in structured folders, Output summary results …"*
   > *(full spec with constraints, input/output format, and architecture
   > instructions)*

2. > *"In order to run in Visual Studio please create a solution file"*

3. > *"Create a new Repo in my Github"*

4. > *"You're not authenticated yet. Run this in your terminal to log in?"*
   > *(asking for help completing GitHub CLI auth)*

5. > *"Verification completed"*
   > *(confirmed GitHub auth; AI proceeded to create repo, commit, and push)*

6. > *"Make it public in order to share with others users"*

7. > *"How to control not to check api keys by any AI Tools"*

8. > *"I am getting this message: There's nothing here, yet."*
   > *(GitHub repo homepage was blank — no README)*

9. > *"I updated api key in appsettings.json and try to run using Visual Studio
   > but looks like its not working"*

10. > *"Response from api: Target = {StatusCode: 404, ReasonPhrase: 'Not Found' …}"*
    > *(NASA API returning 404 for all requests)*

11. > *"What are all rovers available?"*

12. > *"Continue from where you left off."*

13. > *"Cancel the Operation Revert back to first rover changes. Don't want
    > images-api.nasa.gov/search"*

14. > *"Check whether it is best approach or not before hitting api whether already
    > folder exists or not?"*

15. > *"Exclude sub folder from photos to commits"*

16. > *"README.md explains: 1) How to run the project 2) Any assumptions made"*

17. > *"Create a file named AI_NOTES.md containing: 1. The AI tool(s) you used
    > 2. Prompts that used 3. One example where AI generated incorrect or
    > incomplete code and how you fixed it 4. Any significant changes you made
    > after AI generated the code"*

---

## 3. Example Where AI Generated Incorrect or Incomplete Code

### Issue — Visual Studio opened a blank 404 page on F5

**What AI generated (initial `launchSettings.json`):**
```json
{
  "profiles": {
    "MarsRoverPhotos": {
      "commandName": "Project",
      "launchBrowser": true,
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      },
      "applicationUrl": "https://localhost:50474;http://localhost:50475"
    }
  }
}
```

**The problem:**
The `launchUrl` property was missing. When pressing **F5** in Visual Studio,
the browser opened `https://localhost:50474/` — a route that has no controller
mapped to it — so the browser showed a blank 404 page. The application was
actually running correctly, but it looked completely broken.

**How it was fixed:**
Added `"launchUrl": "swagger"` so Visual Studio opens the Swagger UI directly:

```json
{
  "profiles": {
    "MarsRoverPhotos": {
      "commandName": "Project",
      "launchBrowser": true,
      "launchUrl": "swagger",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      },
      "applicationUrl": "https://localhost:50474;http://localhost:50475"
    }
  }
}
```

---

## 4. Significant Changes Made After AI Generated the Code

### 4.1 — Added missing Swashbuckle NuGet package

The AI-generated `Program.cs` called `AddSwaggerGen()` and `UseSwagger()`, but
the `.csproj` did not reference the `Swashbuckle.AspNetCore` package. In .NET 8
Swagger is no longer bundled. The project failed to build until the package was
added:

```bash
dotnet add package Swashbuckle.AspNetCore
```

---

### 4.2 — NASA Mars Rover Photos API was decommissioned

The AI built the `NasaApiClient` against the original endpoint:
```
https://api.nasa.gov/mars-photos/api/v1/rovers/{rover}/photos
```
This API was backed by a community Heroku application
(`corincerami/mars-photo-api`) which was **archived in October 2025**. Every
request returned a Heroku `404 no-such-app` page.

**Attempted fix:** AI switched to the NASA Image and Video Library
(`images-api.nasa.gov/search`) as a working alternative. However this API does
not support exact `earth_date` filtering — only year-level filtering — which
changed the semantics of the application.

**Final decision:** Reverted back to the original `api.nasa.gov/mars-photos`
endpoint per user instruction, keeping the architecture intact for when NASA
restores or replaces the backend. The rover list and selection feature were
retained as a new capability.

---

### 4.3 — API key committed to source control

The AI generated `appsettings.json` with `"ApiKey": "DEMO_KEY"` as a
placeholder, which is safe. However the user replaced this with their real
NASA API key directly in the file, which was then committed to GitHub.

**Fix applied:**
- Replaced the real key in `appsettings.json` with the placeholder
  `YOUR_NASA_API_KEY_HERE`
- Moved the real key to **.NET User Secrets** (stored in
  `%APPDATA%\Microsoft\UserSecrets\` — never committed)
- Added `.claudeignore` to prevent AI tools reading secret files
- Confirmed GitHub secret scanning and push protection were already enabled

---

### 4.4 — `IsAlreadyDownloaded` check was in the wrong place

The AI placed the folder-existence check **inside `ImageDownloadService`**,
which is called only after the NASA API has already been called. This meant
a full network round-trip was made even for dates whose photos were already
on disk.

**Fix:** Moved the check to `RoverPhotoOrchestrator` so it runs **before**
`GetPhotosAsync` is called. If `photos/{date}/` already contains at least
`MinPhotos` files, the NASA API call is skipped entirely.

```
Before:  Call NASA API → get photos → check folder → skip download
After:   Check folder → already downloaded? return immediately
                      → not downloaded? Call NASA API → download
```

---

### 4.5 — `photos/` folder was committed to git

After running the API, the downloaded images were automatically staged and
committed to GitHub as binary files. The `.gitignore` did not exclude them.

**Fix:**
- Added `MarsRoverPhotos/photos/` to `.gitignore`
- Ran `git rm -r --cached MarsRoverPhotos/photos/` to remove the 15 already-
  tracked JPG files from the index (files were kept on disk)

---

### 4.6 — Rover selection feature added post-generation

The original code hardcoded `curiosity` as the rover in `NasaApiSettings`.
After the user asked *"what are all rovers available?"*, the following was added
beyond the initial AI output:

- `MarsRover` model with name, status, landing date, and description
- `INasaApiClient.GetAvailableRovers()` returning all five rovers
- `GET /api/rover-photos/rovers` endpoint
- Optional `?rover=` query parameter on `POST /api/rover-photos/process`
- Rover name validation with a `400` response listing valid options
- `rover` parameter threaded through the orchestrator and service layer
