using System.Text;
using System.Text.Json;

namespace RemotePrintCore.Web.Services.Notifications;

public class ViberNotificationChannel
{
    private readonly string _authToken;
    private readonly string _senderName;
    private readonly ILogger<ViberNotificationChannel> _logger;

    public ViberNotificationChannel(IConfiguration config, ILogger<ViberNotificationChannel> logger)
    {
        var section = config.GetSection("Notifications:Viber");
        _authToken = section["AuthToken"] ?? string.Empty;
        _senderName = section["SenderName"] ?? "RemotePrintCore";
        _logger = logger;
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_authToken);

    public async Task SendAsync(string userId, string documentNumber, IHttpClientFactory httpClientFactory)
    {
        var payload = new
        {
            receiver = userId,
            min_api_version = 1,
            sender = new { name = _senderName },
            tracking_data = documentNumber,
            type = "text",
            text = $"Document {documentNumber} has been printed.",
        };

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var client = httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Viber-Auth-Token", _authToken);

        var response = await client.PostAsync("https://chatapi.viber.com/pa/send_message", content);
        response.EnsureSuccessStatusCode();

        _logger.LogInformation("Viber notification sent for document {DocumentNumber} to user {UserId}",
            documentNumber, userId);
    }
}
