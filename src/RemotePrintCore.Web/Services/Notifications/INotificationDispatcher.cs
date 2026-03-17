namespace RemotePrintCore.Web.Services.Notifications;

using RemotePrintCore.Web.Models.Soap;

public interface INotificationDispatcher
{
    Task DispatchAsync(byte[] pdfBytes, string documentNumber, NotificationOptions options);
}
