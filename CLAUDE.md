# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

PISM (Private Image Storage Management) is an ASP.NET Core 8 web application for scanning, organizing, reviewing, and securely storing image files. Images are encrypted at rest (AES-256-GCM) and decrypted only when displayed. It runs in Docker with a PostgreSQL database.

## Solution Structure

Four projects in `PISM.slnx` — always build with `dotnet build PISM.slnx`:
- **PISM.Web** — ASP.NET Core MVC app (UI + SignalR hub + scan background service)
- **PISM.Worker** — Standalone background worker (alternative deployment without SignalR)
- **PISM.Core** — Models, enums, options, service interfaces, and service implementations
- **PISM.Data** — EF Core DbContext, repositories, scanner/reset services, migrations

Project references: Web → Core + Data; Worker → Core + Data; Data → Core.

## Key Source Locations

### PISM.Core
- `Models/ImageFile.cs` — core entity: filename, folder, hash, status, encryption path, crop/rotation, duplicate flag
- `Models/ImageTag.cs` — tag string on an `ImageFile` (FK: `ImageFileId`)
- `Models/DeletedHash.cs` — permanent hash record for auto-deleting future duplicates
- `Models/ScanJob.cs` — folder scan tracking: counters, status, error message
- `Models/ScanProgressUpdate.cs` — SignalR payload for real-time scan progress
- `Enums/ImageStatus.cs` — `NeedsReview`, `Kept`, `Deleted`
- `Enums/ScanJobStatus.cs` — `Pending`, `Running`, `Completed`, `Failed`
- `Options/StorageOptions.cs` — `Storage:RootPath` (configurable encrypted file storage path)
- `Options/EncryptionOptions.cs` — `Encryption:Key` (Base64 32-byte AES-256 key)
- `Options/TestingOptions.cs` — `Testing:PreserveOriginals` (enables test mode + reset endpoint)
- `Services/IEncryptionService.cs` + `AesGcmEncryptionService.cs` — AES-256-GCM; file format `[nonce(12)][tag(16)][ciphertext]`
- `Services/IFileStorageService.cs` + `LocalFileStorageService.cs` — stores encrypted files at `{root}/{hash[0..1]}/{hash[2..3]}/{hash}.enc`; supports UNC paths
- `Services/IScannerService.cs` — enqueue jobs, process next pending, get active, reset stuck jobs

### PISM.Data
- `PismDbContext.cs` — DbSets: `ImageFiles`, `ImageTags`, `DeletedHashes`, `ScanJobs`
- `DataServiceExtensions.cs` — `AddPismData(connectionString)` registers DbContext, all repositories, `IScannerService`, and `ResetService`; called from both Web and Worker `Program.cs`
- `PismDbContextFactory.cs` — design-time factory for `dotnet ef` CLI
- `Migrations/` — EF Core migrations
- `Repositories/IImageRepository.cs` + `ImageRepository.cs` — get by hash/id, paged review queue, add/update, counts/sizes by status
- `Repositories/IScanJobRepository.cs` + `ScanJobRepository.cs` — CRUD for scan jobs, reset running jobs on startup
- `Services/ScannerService.cs` — full scan pipeline: SHA-256 hash → check deleted hashes → check duplicates → AES encrypt → store → optionally delete original → write sidecar in test mode
- `Services/ResetService.cs` — wipes all DB rows (FK-safe order), deletes all encrypted files, removes sidecar files; used by reset endpoint

### PISM.Web
- `Program.cs` — registers options, encryption/storage services (singleton), `AddPismData`, SignalR, `ScanBackgroundService`, MVC
- `Hubs/ScanHub.cs` — strongly-typed `Hub<IScanHubClient>`; mounted at `/hubs/scan`; client methods: `ScanProgressUpdated`, `ScanJobCompleted`, `ScanJobFailed`
- `Services/ScanBackgroundService.cs` — polls for pending scan jobs every 5s; pushes SignalR updates; resets stuck jobs on startup
- `Controllers/AdminController.cs` — `/admin/reset` GET/POST; only accessible when `Testing:PreserveOriginals = true`
- `Views/Admin/` — reset confirmation and result views

### PISM.Worker
- `Worker.cs` — standalone scan runner using `IScannerService`; logs progress to console; no SignalR

## Tech Stack

- .NET 8, ASP.NET Core MVC, SixLabors.ImageSharp (image transforms, planned)
- Entity Framework Core 8.0.11 + Npgsql 8.0.11 (PostgreSQL)
- SignalR (built into ASP.NET Core shared framework — no separate NuGet package)
- Docker + docker-compose (app + PostgreSQL 16 container)
- Anthropic Claude API (planned, for AI-assisted features)

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
| `Anthropic__ApiKey` | Anthropic Claude API key (planned) |

Copy `.env.example` to `.env` for local overrides (`.env` is gitignored).
