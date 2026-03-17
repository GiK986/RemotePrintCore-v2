using Microsoft.AspNetCore.Hosting;
using System.Text.RegularExpressions;

namespace RemotePrintCore.Web.Services.Pdf;

public class DocumentGenerator : IDocumentGenerator
{
    private readonly IViewRenderService _viewRenderService;
    private readonly IHtmlToPdfConverter _htmlToPdfConverter;
    private readonly string _wwwrootPath;

    public DocumentGenerator(
        IViewRenderService viewRenderService,
        IHtmlToPdfConverter htmlToPdfConverter,
        IWebHostEnvironment env)
    {
        _viewRenderService = viewRenderService;
        _htmlToPdfConverter = htmlToPdfConverter;
        _wwwrootPath = env.WebRootPath;
    }

    public async Task<byte[]> GenerateAsync(string templateName, object model)
    {
        var html = await _viewRenderService.RenderToStringAsync(
            $"~/Views/Export/{templateName}.cshtml", model);

        html = InlineLocalAssets(html);

        return await _htmlToPdfConverter.ConvertAsync(html);
    }

    private string InlineLocalAssets(string html)
    {
        // Replace <link href="/css/..."> with inline <style>
        html = Regex.Replace(html,
            @"<link[^>]+href=""(/css/[^""]+)""[^>]*/?>",
            m =>
            {
                var relativePath = m.Groups[1].Value.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
                var fullPath = Path.Combine(_wwwrootPath, relativePath);
                return File.Exists(fullPath)
                    ? $"<style>{File.ReadAllText(fullPath)}</style>"
                    : m.Value;
            },
            RegexOptions.IgnoreCase);

        // Replace src="/image/..." and src="/uploads/..." with base64 data URIs
        html = Regex.Replace(html,
            @"src=""(/(image|uploads)/[^""]+)""",
            m =>
            {
                var relativePath = m.Groups[1].Value.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
                var fullPath = Path.Combine(_wwwrootPath, relativePath);
                if (!File.Exists(fullPath)) return m.Value;

                var ext = Path.GetExtension(fullPath).TrimStart('.').ToLowerInvariant();
                var mime = ext is "jpg" or "jpeg" ? "image/jpeg" : $"image/{ext}";
                var base64 = Convert.ToBase64String(File.ReadAllBytes(fullPath));
                return $@"src=""data:{mime};base64,{base64}""";
            },
            RegexOptions.IgnoreCase);

        return html;
    }
}
