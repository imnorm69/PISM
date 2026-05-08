# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

PISM (Private Image Storage Management) is an ASP.NET Core web application for scanning, organizing, reviewing, and securely storing image files. It runs in Docker with a PostgreSQL database.

## Solution Structure

Four projects in `PISM.slnx`:
- **PISM.Web** — ASP.NET Core MVC app (UI + SignalR hub + API endpoints)
- **PISM.Worker** — Background/Hosted Service for folder scanning
- **PISM.Core** — Shared models, enums, interfaces, and DTOs
- **PISM.Data** — EF Core DbContext, migrations, and service registration

Project references: Web → Core + Data; Worker → Core + Data; Data → Core.

## Key Source Locations

- `PISM.Core/Models/` — `ImageFile`, `Tag`
- `PISM.Core/Enums/` — `ImageStatus` (`Pending`, `Kept`, `Deleted`)
- `PISM.Data/PismDbContext.cs` — EF Core DbContext with `ImageFiles` and `Tags` DbSets
- `PISM.Data/DataServiceExtensions.cs` — `AddPismData(connectionString)` extension used by Web and Worker
- `PISM.Data/PismDbContextFactory.cs` — design-time factory for `dotnet ef` CLI
- `PISM.Data/Migrations/` — EF Core migrations
- `PISM.Web/Hubs/ScanHub.cs` — SignalR hub for scan progress; mounted at `/hubs/scan`

## Tech Stack

- .NET 8, ASP.NET Core MVC
- Entity Framework Core 8.0.11 + Npgsql 8.0.11 (PostgreSQL)
- SignalR (built into ASP.NET Core shared framework — no separate NuGet package)
- Docker + docker-compose (app + PostgreSQL 16 container)
- Anthropic Claude API (planned, for AI-assisted features)

## Build & Run

```bash
dotnet build
dotnet test
docker-compose up --build
```

### EF Core migrations

```bash
dotnet ef migrations add <Name> --project PISM.Data
dotnet ef database update --project PISM.Data
```

## Package Version Notes

All EF Core packages are pinned to `8.0.11` to match `Npgsql.EntityFrameworkCore.PostgreSQL 8.0.11`. Do not float these with `8.*` — mismatched patch versions cause MSB3277 assembly conflict warnings.

## Environment Variables

| Variable | Purpose |
|---|---|
| `ConnectionStrings__Default` | PostgreSQL connection string |
| `Anthropic__ApiKey` | Anthropic Claude API key |

Copy `.env.example` to `.env` for local overrides (`.env` is gitignored).
