# Log Retention Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Автоматично изтриване на файлови логове и PrintLog записи по-стари от 14 дни.

**Architecture:** Serilog `retainedFileCountLimit` ограничава броя rolling файлове. `LogCleanupService` (BackgroundService) изчаква до следващата полунощ (Europe/Sofia) и изпълнява hard delete на стари PrintLog записи всеки ден. Retention периодът е конфигурируем от `appsettings.json`.

**Tech Stack:** .NET 10 BackgroundService, EF Core 10 `ExecuteDeleteAsync`, Serilog

---

### Task 1: Serilog файлово задържане

**Files:**
- Modify: `src/RemotePrintCore.Web/Program.cs:19`

**Step 1: Добави `retainedFileCountLimit: 14`**

Замени ред 19 в `Program.cs`:

```csharp
// Преди:
.WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)

// След:
.WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 14)
```

**Step 2: Commit**

```bash
git add src/RemotePrintCore.Web/Program.cs
git commit -m "feat: limit Serilog rolling files to 14"
```

---

### Task 2: Конфигурация за retention период

**Files:**
- Modify: `src/RemotePrintCore.Web/appsettings.json`

**Step 1: Добави секция `LogRetention`**

В `appsettings.json` добави след `"AllowedHosts"`:

```json
"LogRetention": {
  "RetentionDays": 14
}
```

**Step 2: Commit**

```bash
git add src/RemotePrintCore.Web/appsettings.json
git commit -m "feat: add LogRetention config section"
```

---

### Task 3: LogCleanupService

**Files:**
- Create: `src/RemotePrintCore.Web/Services/LogCleanupService.cs`

**Step 1: Създай файла**

```csharp
using Microsoft.EntityFrameworkCore;
using RemotePrintCore.Web.Data;

namespace RemotePrintCore.Web.Services;

public class LogCleanupService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<LogCleanupService> _logger;
    private readonly int _retentionDays;
    private static readonly TimeZoneInfo _tz = TimeZoneInfo.FindSystemTimeZoneById("Europe/Sofia");

    public LogCleanupService(
        IServiceScopeFactory scopeFactory,
        ILogger<LogCleanupService> logger,
        IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _retentionDays = configuration.GetValue<int>("LogRetention:RetentionDays", 14);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = GetDelayUntilNextMidnight();
            _logger.LogInformation("Log cleanup scheduled in {Delay}", delay);
            await Task.Delay(delay, stoppingToken);

            if (!stoppingToken.IsCancellationRequested)
                await CleanupAsync(stoppingToken);
        }
    }

    private static TimeSpan GetDelayUntilNextMidnight()
    {
        var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _tz);
        var nextMidnight = now.Date.AddDays(1);
        return nextMidnight - now;
    }

    private async Task CleanupAsync(CancellationToken stoppingToken)
    {
        var cutoff = DateTime.UtcNow.AddDays(-_retentionDays);

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var deleted = await db.PrintLogs
            .IgnoreQueryFilters()
            .Where(x => x.CreatedOn < cutoff)
            .ExecuteDeleteAsync(stoppingToken);

        _logger.LogInformation(
            "Log cleanup: deleted {Count} PrintLog records older than {Days} days",
            deleted, _retentionDays);
    }
}
```

> **Забележка:** `IgnoreQueryFilters()` е необходимо защото `AppDbContext` има глобален филтър за soft deletes (`IsDeleted`). Без него soft-deleted записи няма да бъдат изтрити.

**Step 2: Commit**

```bash
git add src/RemotePrintCore.Web/Services/LogCleanupService.cs
git commit -m "feat: add LogCleanupService for nightly PrintLog cleanup"
```

---

### Task 4: Регистрация в Program.cs

**Files:**
- Modify: `src/RemotePrintCore.Web/Program.cs`

**Step 1: Добави `AddHostedService` след останалите `AddScoped` регистрации**

В `Program.cs`, след реда с `AddScoped<INotificationDispatcher, ...>()`:

```csharp
builder.Services.AddHostedService<LogCleanupService>();
```

**Step 2: Добави using (ако е нужно)**

`LogCleanupService` е в `RemotePrintCore.Web.Services` namespace — същото като останалите services, така че не трябва допълнителен `using`.

**Step 3: Build проверка**

```bash
dotnet build src/RemotePrintCore.Web/RemotePrintCore.Web.csproj
```

Очаквано: `Build succeeded.`

**Step 4: Commit**

```bash
git add src/RemotePrintCore.Web/Program.cs
git commit -m "feat: register LogCleanupService as hosted service"
```
