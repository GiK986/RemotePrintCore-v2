namespace RemotePrintCore.Web.Models.Soap;

[Serializable]
public class SenderInformation
{
    public string FullName { get; set; } = string.Empty;

    public string Adrress { get; set; } = string.Empty;
}
