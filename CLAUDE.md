# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```bash
# Build
dotnet build src/RemotePrintCore.Web/RemotePrintCore.Web.csproj

# Run locally (requires Playwright Chromium installed)
dotnet run --project src/RemotePrintCore.Web

# Publish release build
dotnet publish src/RemotePrintCore.Web -c Release -o ./publish

# Run via Docker (requires .env file with credentials)
docker compose up -d

# EF Core migrations
dotnet ef migrations add <Name> --project src/RemotePrintCore.Web
dotnet ef database update --project src/RemotePrintCore.Web

# Install Playwright Chromium (required for local dev)
dotnet tool install --global Microsoft.Playwright.CLI
playwright install chromium
```

No test projects exist in this repository.

## Architecture

RemotePrintCore v2 is an ASP.NET Core 10 / Blazor Server application that receives print jobs via SOAP, generates PDFs, and sends them to network printers via TCP.

**Core request flow:**

1. ERP system calls `PrintDocumentInfo()` on the SOAP endpoint (`/Service.asmx`, hosted via SoapCore)
2. `SampleService` orchestrates the full pipeline: map → render → generate PDF → print → notify → log
3. `DocumentMapper` converts SOAP DTOs (`Models/Soap/`) to Razor ViewModels (`Models/ViewModels/`)
4. `DocumentGenerator` renders a Razor view (from `Views/Export/`) to HTML, inlines assets, then calls `PlaywrightPdfConverter` (headless Chromium) to produce a PDF
5. `BannerService` injects a random active banner image into the PDF
6. `TcpPrinterService` sends raw PDF bytes to the physical printer via TCP
7. `NotificationDispatcher` optionally sends email (MailKit), Telegram, or Viber notifications
8. `PrintLogService` records the event in SQLite

**UI layer:** Blazor Server admin UI (protected by ASP.NET Core Identity) for managing printers, banners, document templates, print logs, and users. Uses MudBlazor for Material Design components. Authentication uses Razor Pages (`Pages/Account/`). Banner file uploads go through a REST endpoint (`/api/banner-upload`) to avoid SignalR size limits.

**Data layer:** EF Core 10 with SQLite (`data/app.db`). All entities extend `BaseEntity` which provides soft deletes (`IsDeleted`, `DeletedOn`) and audit fields (`CreatedOn`, `ModifiedOn`) — `AppDbContext` applies global query filters for soft deletes and auto-sets audit timestamps in `SaveChanges`.

**Key directories:**

- `Services/` — Business logic organized by concern (Pdf, Printing, Notifications, Banners, Templates, PrintLog, Soap, Mapping)
- `Models/Entities/` — EF Core database models
- `Models/Soap/` — SOAP request/response DTOs (contract with ERP system)
- `Models/ViewModels/` — Razor template binding models
- `Views/Export/` — Razor `.cshtml` templates for PDF generation (DocumentTemplateV1, V1.1, V2, V3, TR variants)
- `Components/Pages/` — Blazor UI pages
- `Migrations/` — EF Core migration history

**Document templates:** Multiple layout variants (`V1`, `V1.1`, `V2`, `V3`, `TR`) are stored in the DB and selected per print job via `PrinterOption.Template`. The `DocumentTemplateTR` variant skips printing if the document contains a 'зареждане' comment.

**Configuration:** Runtime config lives in `appsettings.json` (SMTP, Telegram token, Viber token, connection string). Secrets for Docker deployment go in `.env` (see `.env.example`). Serilog writes structured logs to console and rolling daily files under `logs/`.

**Deployment:** Multi-stage Dockerfile builds the app and installs Playwright Chromium in the runtime stage. GitHub Actions (`.github/workflows/docker-publish.yml`) builds and pushes a Docker image to Docker Hub on every push to `main`. Traefik handles TLS termination externally.
