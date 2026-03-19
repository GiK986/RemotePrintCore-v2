using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace RemotePrintCore.Web.Services.Notifications;

public class EmailNotificationChannel
{
    private readonly string _host;
    private readonly int _port;
    private readonly string _username;
    private readonly string _password;
    private readonly string _fromAddress;
    private readonly string _fromName;
    private readonly bool _ignoreSslErrors;
    private readonly ILogger<EmailNotificationChannel> _logger;

    public EmailNotificationChannel(IConfiguration config, ILogger<EmailNotificationChannel> logger)
    {
        var section = config.GetSection("Notifications:Email");
        _host = section["SmtpHost"] ?? string.Empty;
        _port = section.GetValue<int>("SmtpPort");
        _username = section["Username"] ?? string.Empty;
        _password = section["Password"] ?? string.Empty;
        _fromAddress = section["FromAddress"] ?? string.Empty;
        _fromName = section["FromName"] ?? "RemotePrintCore";
        _ignoreSslErrors = section.GetValue<bool>("IgnoreSslErrors");
        _logger = logger;
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_host) && !string.IsNullOrWhiteSpace(_fromAddress);

    public async Task SendAsync(string toEmail, string documentNumber, byte[] pdfBytes)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_fromName, _fromAddress));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = $"Документ №{documentNumber}";

        var builder = new BodyBuilder
        {
            TextBody = $"""
                Здравейте,

                Моля, намерете приложен документ с номер {documentNumber}.

                ──────────────────────────────
                Този имейл е генериран автоматично.
                Моля, не отговаряйте на него.
                ──────────────────────────────
                """,
        };
        builder.Attachments.Add($"{documentNumber}.pdf", pdfBytes, new ContentType("application", "pdf"));
        message.Body = builder.ToMessageBody();

        using var client = new SmtpClient();

        if (_ignoreSslErrors)
            client.ServerCertificateValidationCallback = (_, _, _, _) => true;

        var sslOptions = _port == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTlsWhenAvailable;
        await client.ConnectAsync(_host, _port, sslOptions);

        if (!string.IsNullOrWhiteSpace(_username))
            await client.AuthenticateAsync(_username, _password);

        await client.SendAsync(message);
        await client.DisconnectAsync(true);

        _logger.LogInformation("Email notification sent for document {DocumentNumber} to {Email}",
            documentNumber, toEmail);
    }
}
