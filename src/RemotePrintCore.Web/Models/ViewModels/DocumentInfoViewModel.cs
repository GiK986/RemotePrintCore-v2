namespace RemotePrintCore.Web.Models.ViewModels;

public class DocumentInfoViewModel
{
    public DocumentHeaderViewModel DocumentHeader { get; set; }
    public DocumentItemViewModel[] DocumentItems { get; set; }
    public DocumentFooterViewModel DocumentFooter { get; set; }
    public bool IsSofiaTransit { get; set; }
    public string FileName { get; set; }
    public double TotalDocumentRetailPrice => Math.Round(DocumentItems.Sum(x => x.TotalRetailPrice), 2);
}
