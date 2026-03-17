using System.ServiceModel;
using RemotePrintCore.Web.Models.Soap;

namespace RemotePrintCore.Web.Services.Soap;

[ServiceContract]
public interface ISampleService
{
    [OperationContract]
    string Test(string s);

    [OperationContract]
    Task<bool> PrintDocumentInfo(DocumentInfo documentInfo, PrinterOption printerOption, NotificationOptions? notificationOptions = null);

    [OperationContract]
    Task<string[]> GetAllInstalledPrinters();

    [OperationContract]
    string[] GetAllDocumentTemplateNames();
}
