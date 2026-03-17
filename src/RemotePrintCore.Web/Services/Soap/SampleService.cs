using RemotePrintCore.Web.Models.Entities;
using RemotePrintCore.Web.Models.Soap;
using RemotePrintCore.Web.Services.Banners;
using RemotePrintCore.Web.Services.Mapping;
using RemotePrintCore.Web.Services.Notifications;
using RemotePrintCore.Web.Services.Pdf;
using RemotePrintCore.Web.Services.PrintLog;
using RemotePrintCore.Web.Services.Printing;
using RemotePrintCore.Web.Services.Templates;

namespace RemotePrintCore.Web.Services.Soap;

public class SampleService : ISampleService
{
    private readonly IDocumentGenerator _documentGenerator;
    private readonly IPrinterService _printerService;
    private readonly IBannerService _bannerService;
    private readonly IDocumentTemplatesService _documentTemplatesService;
    private readonly IPrintLogService _printLogService;
    private readonly INotificationDispatcher _notificationDispatcher;
    private readonly ILogger<SampleService> _logger;

    public SampleService(
        IDocumentGenerator documentGenerator,
        IPrinterService printerService,
        IBannerService bannerService,
        IDocumentTemplatesService documentTemplatesService,
        IPrintLogService printLogService,
        INotificationDispatcher notificationDispatcher,
        ILogger<SampleService> logger)
    {
        _documentGenerator = documentGenerator;
        _printerService = printerService;
        _bannerService = bannerService;
        _documentTemplatesService = documentTemplatesService;
        _printLogService = printLogService;
        _notificationDispatcher = notificationDispatcher;
        _logger = logger;
    }

    public string Test(string s) => $"Test Method Executed! {s}";

    public async Task<string[]> GetAllInstalledPrinters()
    {
        return await _printerService.GetAllPrinterNamesAsync();
    }

    public string[] GetAllDocumentTemplateNames()
    {
        return _documentTemplatesService.GetAllNames();
    }

    public async Task<bool> PrintDocumentInfo(
        DocumentInfo documentInfo,
        PrinterOption printerOption,
        NotificationOptions? notificationOptions = null)
    {
        var printerName = printerOption.PrinterName;
        var copies = printerOption.NumberOfCopies;
        var documentNumber = documentInfo.DocumentHeader?.DocumentNumber ?? string.Empty;
        var customerName = documentInfo.DocumentHeader?.RecipientInformation?.FullName;

        _logger.LogInformation(
            "PrintDocumentInfo started. Document={DocumentNumber} Printer={Printer} Template={Template} Copies={Copies}",
            documentNumber, printerName, printerOption.TemplateName, copies);

        // No copies and no notifications — nothing to do
        if (copies == 0 && notificationOptions == null)
            return true;

        try
        {
            var viewModel = DocumentMapper.Map(documentInfo);
            viewModel.FileName = await _bannerService.GetRandomActiveBannerFileNameAsync() ?? string.Empty;

            var templateNames = _documentTemplatesService.GetValuesByName(printerOption.TemplateName);

            if (copies == 0)
            {
                // Notification-only: generate one PDF and dispatch, skip printing and log
                var pdf = await _documentGenerator.GenerateAsync(templateNames.First().Trim(), viewModel);
                _ = Task.Run(() => _notificationDispatcher.DispatchAsync(
                    pdf, documentNumber, notificationOptions!));
                return true;
            }

            if (!await _printerService.IsPrinterAvailableAsync(printerName))
                throw new InvalidOperationException($"Printer '{printerName}' is not available.");

            byte[]? lastPdf = null;
            foreach (var templateName in templateNames)
            {
                var pdf = await _documentGenerator.GenerateAsync(templateName.Trim(), viewModel);
                await _printerService.PrintAsync(printerName, pdf, copies);
                lastPdf ??= pdf;
            }

            await _printLogService.LogAsync(documentNumber, customerName, printerName,
                printerOption.TemplateName, copies, PrintStatus.Success);

            _logger.LogInformation(
                "PrintDocumentInfo succeeded. Document={DocumentNumber} Printer={Printer}",
                documentNumber, printerName);

            if (notificationOptions != null && lastPdf != null)
            {
                _ = Task.Run(() => _notificationDispatcher.DispatchAsync(
                    lastPdf, documentNumber, notificationOptions));
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "PrintDocumentInfo failed. Document={DocumentNumber} Printer={Printer}",
                documentNumber, printerName);

            await _printLogService.LogAsync(documentNumber, customerName, printerName,
                printerOption.TemplateName, copies, PrintStatus.Error, ex.Message);

            throw;
        }
    }
}
