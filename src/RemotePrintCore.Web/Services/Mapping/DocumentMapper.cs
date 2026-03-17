using RemotePrintCore.Web.Models.Soap;
using RemotePrintCore.Web.Models.ViewModels;

namespace RemotePrintCore.Web.Services.Mapping;

public static class DocumentMapper
{
    public static DocumentInfoViewModel Map(DocumentInfo info) => new()
    {
        IsSofiaTransit = info.IsSofiaTransit,
        DocumentHeader = MapHeader(info.DocumentHeader),
        DocumentItems = info.DocumentItems?.Select(MapItem).ToArray() ?? [],
        DocumentFooter = MapFooter(info.DocumentFooter),
    };

    private static DocumentHeaderViewModel MapHeader(DocumentHeader h) => new()
    {
        DocumentNumber = h.DocumentNumber,
        OrderNumber = h.OrderNumber,
        DocumentData = h.DocumentData,
        DocumentDueData = h.DocumentDueData,
        Comment = h.Comment,
        WarehouseName = h.WarehouseName,
        WarehouseAddress = h.WarehouseAddress,
        RecipientInformation = MapRecipient(h.RecipientInformation),
        SenderInformation = MapSender(h.SenderInformation),
    };

    private static DocumentItemViewModel MapItem(DocumentItem i) => new()
    {
        CodeNumber = i.CodeNumber,
        Description = i.Description,
        Quantity = i.Quantity,
        UnitDemension = i.UnitDemension,
        RetailPrice = i.RetailPrice,
        SalesPrice = i.SalesPrice,
        TotalSalesPrice = i.TotalSalesPrice,
        PaletNumber = i.PaletNumber,
    };

    private static DocumentFooterViewModel MapFooter(DocumentFooter f) => new()
    {
        TotalSalesPrice = f.TotalSalesPrice,
        AuthorName = f.AuthorName,
    };

    private static RecipientInformationViewModel MapRecipient(RecipientInformation r) => new()
    {
        FullName = r.FullName,
        Adrress = r.Adrress,
        TotalAmountDue = r.TotalAmountDue,
    };

    private static SenderInformationViewModel MapSender(SenderInformation s) => new()
    {
        FullName = s.FullName,
        Adrress = s.Adrress,
    };
}
