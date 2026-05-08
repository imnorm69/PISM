# PISM — Private Image Storage Management

ASP.NET Core 8 web application for scanning, organizing, reviewing, and securely storing image files. Images are encrypted at rest and decrypted only when displayed.

---

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8)
- [Docker Desktop](https://www.docker.com/products/docker-desktop)
- `dotnet-ef` tool: `dotnet tool install --global dotnet-ef`

---

## Configuration

All sensitive values are supplied via environment variables or a `.env` file (gitignored). Copy the example file to get started:

```bash
cp .env.example .env
```

| Variable | Description |
|---|---|
| `ConnectionStrings__Default` | PostgreSQL connection string |
| `Storage__RootPath` | Absolute path where encrypted files are stored (UNC paths supported) |
| `Encryption__Key` | Base64-encoded 32-byte AES-256 key |
| `Testing__PreserveOriginals` | `true` to enable test mode (see below) |

### Generating an encryption key

```bash
# PowerShell
[Convert]::ToBase64String((1..32 | ForEach-Object { Get-Random -Maximum 256 }))

# bash / openssl
openssl rand -base64 32
```

---

## Running with Docker

```bash
docker-compose up --build
```

The app will be available at `http://localhost:8080`. PostgreSQL is available at `localhost:5432`.

---

## Local Development (without Docker)

1. Start PostgreSQL locally (or use `docker-compose up db`).
2. Fill in `appsettings.Development.json` with your local values (connection string, storage path, encryption key).
3. Run database migrations:
   ```bash
   dotnet ef database update --project PISM.Data
   ```
4. Run the web app:
   ```bash
   dotnet run --project PISM.Web
   ```

---

## Test Mode & Reset Utility

### Enabling test mode

Set `Testing:PreserveOriginals` to `true` (already set in `appsettings.Development.json`).

When test mode is active:
- Original image files are **not deleted** after encryption — useful for re-running scans during development.
- A `_pism_scanned.txt` sidecar file is written to each scanned folder tracking which files have been processed. The scanner uses this file to skip already-processed files on resume.
- The **Reset** endpoint is enabled.

### Using the Reset endpoint

> **Warning:** The reset is destructive and irreversible. Use only during development.

1. Navigate to `http://localhost:8080/admin/reset` (only accessible when `Testing:PreserveOriginals = true`).
2. Review the warning page listing exactly what will be deleted.
3. Click **Reset Everything** to confirm.

The reset will:
- Delete all rows from `ImageFiles`, `ImageTags`, `DeletedHashes`, and `ScanJobs`.
- Delete all encrypted files from the configured storage folder.
- Delete any `_pism_scanned.txt` sidecar files from previously scanned folders.

After a reset, the app is in a clean state as if it had never been run. You can then re-scan your original test images from scratch.

---

## EF Core Migrations

```bash
# Add a new migration
dotnet ef migrations add <MigrationName> --project PISM.Data

# Apply migrations to the database
dotnet ef database update --project PISM.Data
```
