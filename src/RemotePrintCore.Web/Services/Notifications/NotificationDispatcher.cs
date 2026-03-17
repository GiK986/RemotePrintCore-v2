using RemotePrintCore.Web.Models.Soap;

namespace RemotePrintCore.Web.Services.Notifications;

public class NotificationDispatcher : INotificationDispatcher
{
    private readonly EmailNotificationChannel _email;
    private readonly TelegramNotificationChannel _telegram;
    private readonly ViberNotificationChannel _viber;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<NotificationDispatcher> _logger;

    public NotificationDispatcher(
        IConfiguration config,
        IHttpClientFactory httpClientFactory,
        ILogger<NotificationDispatcher> logger,
        ILogger<EmailNotificationChannel> emailLogger,
        ILogger<TelegramNotificationChannel> telegramLogger,
        ILogger<ViberNotificationChannel> viberLogger)
    {
        _email = new EmailNotificationChannel(config, emailLogger);
        _telegram = new TelegramNotificationChannel(config, telegramLogger);
        _viber = new ViberNotificationChannel(config, viberLogger);
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task DispatchAsync(byte[] pdfBytes, string documentNumber, NotificationOptions options)
    {
        var tasks = new List<Task>();

        if (!string.IsNullOrWhiteSpace(options.Email) && _email.IsConfigured)
            tasks.Add(RunSafe(() => _email.SendAsync(options.Email, documentNumber, pdfBytes), "Email"));

        if (!string.IsNullOrWhiteSpace(options.TelegramChatId) && _telegram.IsConfigured)
            tasks.Add(RunSafe(() => _telegram.SendAsync(options.TelegramChatId, documentNumber, pdfBytes), "Telegram"));

        if (!string.IsNullOrWhiteSpace(options.ViberUserId) && _viber.IsConfigured)
            tasks.Add(RunSafe(() => _viber.SendAsync(options.ViberUserId, documentNumber, _httpClientFactory), "Viber"));

        await Task.WhenAll(tasks);
    }

    private async Task RunSafe(Func<Task> action, string channel)
    {
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Channel} notification failed", channel);
        }
    }
}
