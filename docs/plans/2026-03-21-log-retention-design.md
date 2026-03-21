# Log Retention Design

**Date:** 2026-03-21

## Problem

Log files and PrintLog database records accumulate indefinitely. Goal: retain no more than 14 days of data for both.

## Solution

### 1. Serilog File Retention

Add `retainedFileCountLimit: 14` to the `WriteTo.File(...)` call in `Program.cs`. Serilog deletes the oldest rolling file automatically each night when a new one is created.

### 2. PrintLog Database Cleanup — `LogCleanupService`

A new `BackgroundService` (`Services/LogCleanupService.cs`) that:

- On startup, calculates the delay until the next midnight (Europe/Sofia timezone) and waits
- Every 24 hours, hard-deletes `PrintLog` records where `CreatedOn < now - retentionDays`
- Uses `IServiceScopeFactory` to create a scoped `AppDbContext` (required for hosted services)
- Retention period is configurable via `appsettings.json` under `LogRetention:RetentionDays` (default: 14)

Registered in `Program.cs` via `AddHostedService<LogCleanupService>()`.

## Configuration

```json
"LogRetention": {
  "RetentionDays": 14
}
```

## Files Changed

- `Program.cs` — add `retainedFileCountLimit`, register `LogCleanupService`
- `Services/LogCleanupService.cs` — new file
- `appsettings.json` — add `LogRetention` section
