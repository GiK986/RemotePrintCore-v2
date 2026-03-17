namespace RemotePrintCore.Web.Services.Printing;

public interface IPrinterService
{
    Task PrintAsync(string printerName, byte[] data, int copies);
    Task<string[]> GetAllPrinterNamesAsync();
    Task<bool> IsPrinterAvailableAsync(string printerName);
    Task<bool> TestConnectionAsync(string ipAddress, int port);
}
