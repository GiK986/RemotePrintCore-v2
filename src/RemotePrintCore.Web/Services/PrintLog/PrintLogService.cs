using Microsoft.EntityFrameworkCore;
using RemotePrintCore.Web.Data;
using RemotePrintCore.Web.Models.Entities;

namespace RemotePrintCore.Web.Services.PrintLog;

public class PrintLogService : IPrintLogService
{
    private readonly AppDbContext _db;

    public PrintLogService(AppDbContext db)
    {
        _db = db;
    }

    public async Task LogAsync(string documentNumber, string? customerName, string printerName,
        string templateName, int copies, PrintStatus status, string? errorMessage = null)
    {
        _db.PrintLogs.Add(new Models.Entities.PrintLog
        {
            DocumentNumber = documentNumber,
            CustomerName = customerName,
            PrinterName = printerName,
            TemplateName = templateName,
            NumberOfCopies = copies,
            Status = status,
            ErrorMessage = errorMessage,
        });

        await _db.SaveChangesAsync();
    }

    public async Task<List<Models.Entities.PrintLog>> GetRecentAsync(int count = 10)
    {
        return await _db.PrintLogs
            .OrderByDescending(l => l.CreatedOn)
            .Take(count)
            .ToListAsync();
    }

    public async Task<List<Models.Entities.PrintLog>> SearchAsync(
        DateTime? from, DateTime? to, string? printerName, string? templateName, PrintStatus? status)
    {
        var query = _db.PrintLogs.AsQueryable();

        if (from.HasValue)
            query = query.Where(l => l.CreatedOn >= from.Value);

        if (to.HasValue)
            query = query.Where(l => l.CreatedOn <= to.Value);

        if (!string.IsNullOrEmpty(printerName))
            query = query.Where(l => l.PrinterName == printerName);

        if (!string.IsNullOrEmpty(templateName))
            query = query.Where(l => l.TemplateName == templateName);

        if (status.HasValue)
            query = query.Where(l => l.Status == status.Value);

        return await query
            .OrderByDescending(l => l.CreatedOn)
            .ToListAsync();
    }
}
