namespace RemotePrintCore.Web.Models.ViewModels;

public class DocumentHeaderViewModel
{
    public RecipientInformationViewModel RecipientInformation { get; set; }
    public SenderInformationViewModel SenderInformation { get; set; }
    public string WarehouseName { get; set; }
    public string WarehouseAddress { get; set; }
    public string DocumentNumber { get; set; }
    public string OrderNumber { get; set; }
    public DateTime DocumentData { get; set; }
    public string Comment { get; set; }
    public string DocumentDueData { get; set; }
}
