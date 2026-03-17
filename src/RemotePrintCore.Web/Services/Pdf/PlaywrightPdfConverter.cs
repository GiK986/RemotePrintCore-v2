using Microsoft.Playwright;

namespace RemotePrintCore.Web.Services.Pdf;

public class PlaywrightPdfConverter : IHtmlToPdfConverter, IAsyncDisposable
{
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    public PlaywrightPdfConverter() { }

    private async Task EnsureInitializedAsync()
    {
        if (_browser != null) return;

        await _initLock.WaitAsync();
        try
        {
            if (_browser != null) return;
            _playwright = await Playwright.CreateAsync();
            _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true,
            });
        }
        finally
        {
            _initLock.Release();
        }
    }

    public async Task<byte[]> ConvertAsync(string htmlContent)
    {
        await EnsureInitializedAsync();

        var page = await _browser!.NewPageAsync();
        try
        {
            await page.SetContentAsync(htmlContent, new PageSetContentOptions
            {
                WaitUntil = WaitUntilState.DOMContentLoaded,
            });

            return await page.PdfAsync(new PagePdfOptions
            {
                Format = "A4",
                PrintBackground = true,
            });
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_browser != null)
        {
            await _browser.DisposeAsync();
        }
        _playwright?.Dispose();
    }
}
