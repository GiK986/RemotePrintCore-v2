using System.Net.Sockets;
using Microsoft.EntityFrameworkCore;
using RemotePrintCore.Web.Data;

namespace RemotePrintCore.Web.Services.Printing;

public class TcpPrinterService : IPrinterService
{
    private readonly AppDbContext _db;

    public TcpPrinterService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<string[]> GetAllPrinterNamesAsync()
    {
        return await _db.Printers
            .Where(p => p.IsActive)
            .Select(p => p.Name)
            .ToArrayAsync();
    }

    public async Task<bool> IsPrinterAvailableAsync(string printerName)
    {
        return await _db.Printers
            .AnyAsync(p => p.Name == printerName && p.IsActive);
    }

    public async Task PrintAsync(string printerName, byte[] data, int copies)
    {
        var printer = await _db.Printers
            .FirstOrDefaultAsync(p => p.Name == printerName && p.IsActive)
            ?? throw new InvalidOperationException($"Printer '{printerName}' not found or inactive.");

        for (var i = 0; i < copies; i++)
        {
            using var client = new TcpClient();
            await client.ConnectAsync(printer.IpAddress, printer.Port);
            await using var stream = client.GetStream();
            await stream.WriteAsync(data);
        }
    }

    public async Task<bool> TestConnectionAsync(string ipAddress, int port)
    {
        try
        {
            using var client = new TcpClient();
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            await client.ConnectAsync(ipAddress, port, cts.Token);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
