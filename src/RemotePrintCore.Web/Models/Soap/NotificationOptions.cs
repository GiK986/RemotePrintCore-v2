namespace RemotePrintCore.Web.Models.Soap;

[Serializable]
public class NotificationOptions
{
    public string? Email { get; set; }

    public string? TelegramChatId { get; set; }

    public string? ViberUserId { get; set; }
}
