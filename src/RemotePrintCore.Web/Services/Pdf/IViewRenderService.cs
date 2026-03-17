namespace RemotePrintCore.Web.Services.Pdf;

public interface IViewRenderService
{
    Task<string> RenderToStringAsync(string viewName, object model);
}
