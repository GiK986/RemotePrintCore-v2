using Telegram.Bot;
using Telegram.Bot.Types;

namespace RemotePrintCore.Web.Services.Notifications;

public class TelegramNotificationChannel
{
    private readonly string _botToken;
    private readonly ILogger<TelegramNotificationChannel> _logger;

    public TelegramNotificationChannel(IConfiguration config, ILogger<TelegramNotificationChannel> logger)
    {
        _botToken = config["Notifications:Telegram:BotToken"] ?? string.Empty;
        _logger = logger;
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_botToken);

    public async Task SendAsync(string chatId, string documentNumber, byte[] pdfBytes)
    {
        var bot = new TelegramBotClient(_botToken);

        using var stream = new MemoryStream(pdfBytes);
        await bot.SendDocument(
            chatId: long.Parse(chatId),
            document: InputFile.FromStream(stream, $"{documentNumber}.pdf"),
            caption: $"Document {documentNumber}");

        _logger.LogInformation("Telegram notification sent for document {DocumentNumber} to chat {ChatId}",
            documentNumber, chatId);
    }
}
