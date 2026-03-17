namespace RemotePrintCore.Web.Services.PrintLog;

using RemotePrintCore.Web.Models.Entities;

public interface IPrintLogService
{
    Task LogAsync(string documentNumber, string? customerName, string printerName,
        string templateName, int copies, PrintStatus status, string? errorMessage = null);
    Task<List<Models.Entities.PrintLog>> GetRecentAsync(int count = 10);
    Task<List<Models.Entities.PrintLog>> SearchAsync(DateTime? from, DateTime? to,
        string? printerName, string? templateName, PrintStatus? status);
}
