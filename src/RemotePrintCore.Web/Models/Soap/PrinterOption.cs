namespace RemotePrintCore.Web.Models.Soap;

[Serializable]
public class PrinterOption
{
    public string PrinterName { get; set; } = string.Empty;

    public int NumberOfCopies { get; set; }

    public string TemplateName { get; set; } = string.Empty;
}
