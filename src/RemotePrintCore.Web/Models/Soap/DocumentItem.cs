namespace RemotePrintCore.Web.Models.Soap;

[Serializable]
public class DocumentItem
{
    public string CodeNumber { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public double Quantity { get; set; }

    public string UnitDemension { get; set; } = string.Empty;

    public double RetailPrice { get; set; }

    public double SalesPrice { get; set; }

    public double TotalSalesPrice { get; set; }

    public string PaletNumber { get; set; } = string.Empty;
}
