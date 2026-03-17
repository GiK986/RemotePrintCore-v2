namespace RemotePrintCore.Web.Models.ViewModels;

public class DocumentItemViewModel
{
    public string CodeNumber { get; set; }
    public string Description { get; set; }
    public double Quantity { get; set; }
    public string UnitDemension { get; set; }
    public double RetailPrice { get; set; }
    public double SalesPrice { get; set; }
    public double TotalSalesPrice { get; set; }
    public double TotalRetailPrice => Math.Round(Quantity * RetailPrice, 2);
    public string PaletNumber { get; set; }
}
