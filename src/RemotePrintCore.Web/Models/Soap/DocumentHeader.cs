namespace RemotePrintCore.Web.Models.Soap;

[Serializable]
public class DocumentHeader
{
    public RecipientInformation RecipientInformation { get; set; } = null!;

    public SenderInformation SenderInformation { get; set; } = null!;

    public string WarehouseName { get; set; } = string.Empty;

    public string WarehouseAddress { get; set; } = string.Empty;

    public string DocumentNumber { get; set; } = string.Empty;

    public string OrderNumber { get; set; } = string.Empty;

    public DateTime DocumentData { get; set; }

    public string Comment { get; set; } = string.Empty;

    public string DocumentDueData { get; set; } = string.Empty;
}
