namespace RemotePrintCore.Web.Services.Pdf;

public interface IHtmlToPdfConverter
{
    Task<byte[]> ConvertAsync(string htmlContent);
}
