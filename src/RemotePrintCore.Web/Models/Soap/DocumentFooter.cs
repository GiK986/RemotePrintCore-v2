namespace RemotePrintCore.Web.Models.Soap;

[Serializable]
public class DocumentFooter
{
    public double TotalSalesPrice { get; set; }

    public string AuthorName { get; set; } = string.Empty;
}
