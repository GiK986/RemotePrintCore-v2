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
