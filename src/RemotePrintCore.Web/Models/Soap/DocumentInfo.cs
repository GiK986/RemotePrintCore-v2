namespace RemotePrintCore.Web.Models.Soap;

[Serializable]
public class DocumentInfo
{
    public DocumentHeader DocumentHeader { get; set; } = null!;

    public DocumentItem[] DocumentItems { get; set; } = [];

    public DocumentFooter DocumentFooter { get; set; } = null!;

    public bool IsSofiaTransit { get; set; }
}
