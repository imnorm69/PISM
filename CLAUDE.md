# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

PISM (Private Image Storage Management) is an ASP.NET Core web application for scanning, organizing, reviewing, and securely storing image files. It runs in Docker with a PostgreSQL database.

## Solution Structure

Four projects in `PISM.sln`:
- **PISM.Web** — ASP.NET Core MVC app (UI + SignalR hub + API endpoints)
- **PISM.Worker** — Background/Hosted Service for folder scanning
- **PISM.Core** — Shared models, interfaces, DTOs, and business logic
- **PISM.Data** — EF Core DbContext, migrations, and repositories

## Tech Stack

- .NET 8, ASP.NET Core MVC, Razor Pages
- Entity Framework Core with PostgreSQL (Npgsql provider)
- SignalR for real-time progress updates during scanning
- Docker + docker-compose (app + PostgreSQL container)
- Anthropic Claude API (server-side, for future AI-assisted features)

## Key Features

- Folder import with SHA-256 hashing and duplicate detection
- Background scanning service (runs independently of browser session)
- Review queue: tag files, keep or delete, crop/rotate (non-destructive edits)
- File encryption at rest; decrypted only when displayed in browser
- Encrypted or original file download
- Statistics dashboard (file counts, storage sizes grouped by status)

## Build & Run

```bash
dotnet build PISM.sln
dotnet test
docker-compose up --build
```

## Environment Variables

| Variable | Purpose |
|---|---|
| `ConnectionStrings__Default` | PostgreSQL connection string |
| `Anthropic__ApiKey` | Anthropic Claude API key |
