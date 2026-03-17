namespace RemotePrintCore.Web.Services.Pdf;

public interface IDocumentGenerator
{
    Task<byte[]> GenerateAsync(string templateName, object model);
}
