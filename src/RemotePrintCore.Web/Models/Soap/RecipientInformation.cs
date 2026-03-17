namespace RemotePrintCore.Web.Models.Soap;

[Serializable]
public class RecipientInformation
{
    public string FullName { get; set; } = string.Empty;

    public string Adrress { get; set; } = string.Empty;

    public decimal TotalAmountDue { get; set; }
}
