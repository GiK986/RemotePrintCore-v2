using Microsoft.EntityFrameworkCore;
using RemotePrintCore.Web.Data;

namespace RemotePrintCore.Web.Services.Templates;

public class DocumentTemplatesService : IDocumentTemplatesService
{
    private readonly AppDbContext _db;

    public DocumentTemplatesService(AppDbContext db)
    {
        _db = db;
    }

    public string[] GetAllNames()
    {
        return _db.DocumentTemplates
            .Select(t => t.Name)
            .ToArray();
    }

    public ICollection<string> GetValuesByName(string name)
    {
        var values = _db.DocumentTemplates
            .Where(t => t.Name == name)
            .Select(t => t.Values)
            .FirstOrDefault();

        if (string.IsNullOrEmpty(values))
            throw new ArgumentNullException(nameof(name), $"No template found with name '{name}'.");

        return values.Split(',', StringSplitOptions.RemoveEmptyEntries);
    }
}
