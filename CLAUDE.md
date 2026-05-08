# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

PISM (Private Image Storage Management) is an ASP.NET Core 8 web application for scanning, organizing, reviewing, and securely storing image files. Images are encrypted at rest (AES-256-GCM) and decrypted only when displayed. It runs in Docker with a PostgreSQL database.

## Solution Structure

Four projects in `PISM.slnx` — always build with `dotnet build PISM.slnx`:
- **PISM.Web** — ASP.NET Core MVC app (UI + SignalR hub + scan background service)
- **PISM.Worker** — Standalone background worker (alternative deployment without SignalR)
- **PISM.Core** — Models, enums, options, service interfaces, and service implementations
- **PISM.Data** — EF Core DbContext, repositories, scanner/reset/review services, migrations

Project references: Web → Core + Data; Worker → Core + Data; Data → Core.

## Key Source Locations

### PISM.Core
- `Models/ImageFile.cs` — core entity: filename, folder, hash, status, encryption path, crop/rotation, duplicate flag
- `Models/ImageTag.cs` — tag string on an `ImageFile` (FK: `ImageFileId`)
- `Models/DeletedHash.cs` — permanent hash record for auto-deleting future duplicates
- `Models/ScanJob.cs` — folder scan tracking: counters, status, error message
- `Models/ScanProgressUpdate.cs` — SignalR payload for real-time scan progress
- `Models/FolderContent.cs` — `(FileHash, FolderPath)` pair; unique index; tracks which folder each file was found in
- `Enums/ImageStatus.cs` — `NeedsReview`, `Kept`, `Deleted`
- `Enums/ScanJobStatus.cs` — `Pending`, `Running`, `Completed`, `Failed`
- `Options/StorageOptions.cs` — `Storage:RootPath`
- `Options/EncryptionOptions.cs` — `Encryption:Key` (Base64 32-byte AES-256 key)
- `Options/TestingOptions.cs` — `Testing:PreserveOriginals` (enables test mode + reset endpoint)
- `Services/IEncryptionService.cs` + `AesGcmEncryptionService.cs` — AES-256-GCM; file format `[nonce(12)][tag(16)][ciphertext]`
- `Services/IFileStorageService.cs` + `LocalFileStorageService.cs` — stores encrypted files at `{root}/{hash[0..1]}/{hash[2..3]}/{hash}.enc`; supports UNC paths
- `Services/IScannerService.cs` — enqueue jobs, process next pending, get active, reset stuck jobs

### PISM.Data
- `PismDbContext.cs` — DbSets: `ImageFiles`, `ImageTags`, `DeletedHashes`, `ScanJobs`, `FolderContents`
- `DataServiceExtensions.cs` — `AddPismData(connectionString)` registers DbContext, all repositories, `IScannerService`, `ResetService`, and `ReviewService`; called from both Web and Worker `Program.cs`
- `PismDbContextFactory.cs` — design-time factory for `dotnet ef` CLI
- `Migrations/` — EF Core migrations
- `Repositories/IImageRepository.cs` + `ImageRepository.cs` — get by hash/id/status, paged queues, add/update, counts/sizes by status, `GetAdjacentIdsAsync` (prev/next for detail nav), `GetRelatedAsync` (folder-group related images)
- `Repositories/IScanJobRepository.cs` + `ScanJobRepository.cs` — CRUD for scan jobs, `GetRecentAsync`, reset running jobs on startup
- `Repositories/IFolderContentsRepository.cs` + `FolderContentsRepository.cs` — `UpsertAsync`, `DeleteAsync`, `DeleteByHashAsync`
- `Services/ScannerService.cs` — full scan pipeline: SHA-256 hash → check deleted hashes → check duplicates → AES encrypt → store → write `FolderContents` row → optionally delete original → write sidecar in test mode. Auto-deleted files (DeletedHashes match) do NOT get a FolderContents row.
- `Services/ReviewService.cs` — keep/delete (adds to DeletedHashes + removes FolderContents row), bulk keep/delete, add/remove/bulk tag, save crop+rotation
- `Services/ResetService.cs` — wipes all DB rows (FK-safe order including FolderContents), deletes all encrypted files, removes sidecar files

### PISM.Web
- `Program.cs` — registers options, encryption/storage services (singleton), `AddPismData`, SignalR, `ScanBackgroundService`, MVC
- `Hubs/ScanHub.cs` — strongly-typed `Hub<IScanHubClient>`; mounted at `/hubs/scan`; client methods: `ScanProgressUpdated`, `ScanJobCompleted`, `ScanJobFailed`
- `Services/ScanBackgroundService.cs` — polls for pending scan jobs every 5s; pushes SignalR updates; resets stuck jobs on startup
- `Controllers/ScanController.cs` — `GET/POST /scan` — enqueue folder scan, show active/recent jobs
- `Controllers/ReviewController.cs` — gallery, detail, keep/delete, bulk actions (keep/delete/tag), add/remove tags, save crop; all actions return JSON except gallery/detail page renders
- `Controllers/ImageController.cs` — `GET /image/{id}?mode=thumbnail|preview|original` — serves decrypted image with ImageSharp transforms (thumbnail: 300px max, preview: rotation applied); 1-hour response cache
- `Controllers/DownloadController.cs` — `GET /download/{id}?format=original|edited` (applies crop+rotation via ImageSharp), `POST /download/bulk` (zip stream)
- `Controllers/StatsController.cs` — `GET /stats` — status counts/sizes, duplicates count, recent scan jobs
- `Controllers/AdminController.cs` — `/admin/reset` GET/POST; only accessible when `Testing:PreserveOriginals = true`
- `ViewModels/` — `GalleryPageViewModel`, `DetailViewModel` (includes `RelatedImages`), `StatsViewModel`, `ScanIndexViewModel`
- `Models/BulkActionRequest.cs` — request models for bulk actions, crop save, tag add, bulk download
- `Views/Review/Gallery.cshtml` — 24-up Bootstrap card grid, tabbed NeedsReview/Kept/Deleted, multi-select with bulk toolbar
- `Views/Review/Detail.cshtml` — full image, Cropper.js crop/rotate, keep/delete, tag management, prev/next nav, "Found In Same Folder" related thumbnail strip
- `Views/Scan/Index.cshtml` — folder input form, live SignalR job progress cards, recent job table
- `Views/Stats/Index.cshtml` — status cards, stacked storage bar chart, recent scan jobs
- `Views/Admin/` — reset confirmation and result views
- `Views/Shared/_Layout.cshtml` — dark navbar (Review / Scan / Stats), top scan progress strip (shown when a scan is active), SignalR CDN loaded globally
- `wwwroot/js/scan-progress.js` — SignalR client; updates navbar status indicator and scan job cards in real time
- `wwwroot/js/gallery.js` — checkbox multi-select, quick keep/delete (fades card out), bulk AJAX actions, zip download
- `wwwroot/js/detail.js` — Cropper.js integration, rotation, AJAX keep/delete + auto-advance, tag add/remove

### PISM.Worker
- `Worker.cs` — standalone scan runner using `IScannerService`; logs progress to console; no SignalR

## FolderContents — Related Image Discovery

`FolderContents(FileHash, FolderPath)` with a unique composite index tracks every folder a file hash was found in. The related-image query in `GetRelatedAsync(hash)` is a two-step join:
1. Find all folder paths where `FileHash = hash`
2. Find all other hashes present in any of those folders
3. Return canonical (`IsDuplicate = false`) `ImageFiles` for those hashes

Rows are written by `ScannerService` for every new and duplicate file. Rows are removed by `ReviewService` when an image is deleted. This means: if a duplicate of File A is scanned alongside Files B and C, File A (the original) will show B and C as related images in the detail view.

## Tech Stack

- .NET 8, ASP.NET Core MVC, SixLabors.ImageSharp 3.x (thumbnail + crop/rotate transforms)
- Entity Framework Core 8.0.11 + Npgsql 8.0.11 (PostgreSQL)
- SignalR (built into ASP.NET Core shared framework — no separate NuGet package), SignalR JS client via CDN
- Cropper.js 1.6.2 via CDN (detail view only)
- Docker + docker-compose (app + PostgreSQL 16 container)

## Build & Run

```bash
# Must specify the solution file — dotnet build alone only picks up PISM.Core
dotnet build PISM.slnx

docker-compose up --build
```

### EF Core migrations

```bash
dotnet ef migrations add <Name> --project PISM.Data
dotnet ef database update --project PISM.Data
```

## Test Mode & Reset

Set `Testing:PreserveOriginals = true` (default in `appsettings.Development.json`) to:
- Skip deleting original files after encryption
- Write `_pism_scanned.txt` sidecar files to scanned folders (used for resume on restart)
- Enable `GET/POST /admin/reset` — wipes DB, encrypted files, and sidecars for a clean restart

See `README.md` for full setup and reset instructions.

## Package Version Notes

All EF Core packages are pinned to `8.0.11` to match `Npgsql.EntityFrameworkCore.PostgreSQL 8.0.11`. Do not float these with `8.*` — mismatched patch versions cause MSB3277 assembly conflict warnings.

## Environment Variables

| Variable | Purpose |
|---|---|
| `ConnectionStrings__Default` | PostgreSQL connection string |
| `Storage__RootPath` | Absolute path for encrypted file storage (UNC supported) |
| `Encryption__Key` | Base64-encoded 32-byte AES-256 key |
| `Testing__PreserveOriginals` | `true` to enable test mode |

Copy `.env.example` to `.env` for local overrides (`.env` is gitignored).
