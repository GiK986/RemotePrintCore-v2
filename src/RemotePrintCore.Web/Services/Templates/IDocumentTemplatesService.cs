namespace RemotePrintCore.Web.Services.Templates;

public interface IDocumentTemplatesService
{
    string[] GetAllNames();
    ICollection<string> GetValuesByName(string name);
}
