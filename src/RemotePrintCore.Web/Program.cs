using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using RemotePrintCore.Web.Components;
using RemotePrintCore.Web.Data;
using RemotePrintCore.Web.Models.Entities;
using RemotePrintCore.Web.Services;
using RemotePrintCore.Web.Services.Banners;
using RemotePrintCore.Web.Services.Notifications;
using RemotePrintCore.Web.Services.Pdf;
using RemotePrintCore.Web.Services.Printing;
using RemotePrintCore.Web.Services.PrintLog;
using RemotePrintCore.Web.Services.Soap;
using RemotePrintCore.Web.Services.Templates;
using Serilog;
using SoapCore;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 14)
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog();

// Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default")));

// Identity
builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

// MVC (for Razor view engine used by PDF generation) + Razor Pages (for login/logout)
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// Blazor
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddMudServices();

// SOAP
builder.Services.AddSoapCore();
builder.Services.AddScoped<ISampleService, SampleService>();

// HttpClient
builder.Services.AddHttpClient();

// Application Services
builder.Services.AddScoped<IViewRenderService, ViewRenderService>();
builder.Services.AddSingleton<IHtmlToPdfConverter, PlaywrightPdfConverter>();
builder.Services.AddScoped<IDocumentGenerator, DocumentGenerator>();
builder.Services.AddScoped<IPrinterService, TcpPrinterService>();
builder.Services.AddScoped<IBannerService, BannerService>();
builder.Services.AddScoped<IDocumentTemplatesService, DocumentTemplatesService>();
builder.Services.AddScoped<IPrintLogService, PrintLogService>();
builder.Services.AddScoped<INotificationDispatcher, NotificationDispatcher>();
builder.Services.AddHostedService<LogCleanupService>();

var app = builder.Build();

// Database migration and seeding
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    await RemotePrintCore.Web.Data.Seeding.DbSeeder.SeedAsync(scope.ServiceProvider);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

// SOAP endpoint
((IApplicationBuilder)app).UseSoapEndpoint<ISampleService>(
    "/Service.asmx",
    new SoapEncoderOptions(),
    SoapSerializer.XmlSerializer);

// Razor Pages (login/logout)
app.MapRazorPages();

// Banner file upload (used by drag & drop — bypasses SignalR size limit)
app.MapPost("/api/banner-upload", async (IFormFile file, IWebHostEnvironment env) =>
{
    if (!file.ContentType.StartsWith("image/"))
        return Results.BadRequest("Only images are allowed.");

    var ext = Path.GetExtension(file.FileName);
    var tempName = $"tmp_{Guid.NewGuid()}{ext}";
    var uploadsPath = Path.Combine(env.WebRootPath, "uploads");
    Directory.CreateDirectory(uploadsPath);

    await using var fs = File.Create(Path.Combine(uploadsPath, tempName));
    await file.CopyToAsync(fs);

    return Results.Ok(new { tempFileName = tempName });
}).DisableAntiforgery();

// Template preview (renders cshtml with dummy data for visual inspection)
app.MapGet("/preview/{template}", async (string template, IViewRenderService viewRenderer, IWebHostEnvironment env, IBannerService bannerService) =>
{
    var viewsPath = Path.Combine(env.ContentRootPath, "Views", "Export");
    var valid = Directory.GetFiles(viewsPath, "DocumentTemplate*.cshtml")
        .Select(Path.GetFileNameWithoutExtension)
        .ToHashSet();

    if (!valid.Contains(template))
        return Results.NotFound($"Template '{template}' not found.");

    var model = new RemotePrintCore.Web.Models.ViewModels.DocumentInfoViewModel
    {
        IsSofiaTransit = false,
        FileName = await bannerService.GetRandomActiveBannerFileNameAsync() ?? string.Empty,
        DocumentHeader = new RemotePrintCore.Web.Models.ViewModels.DocumentHeaderViewModel
        {
            DocumentNumber = "000123",
            OrderNumber = "ORD-2026-001",
            DocumentData = DateTime.Now,
            DocumentDueData = "30.03.2026",
            WarehouseName = "Склад София",
            WarehouseAddress = "ул. Примерна 1, София",
            Comment = "Примерен коментар към документа",
            RecipientInformation = new RemotePrintCore.Web.Models.ViewModels.RecipientInformationViewModel
            {
                FullName = "Иван Иванов ООД",
                Adrress = "бул. Витоша 100, София 1000",
                TotalAmountDue = 1250.00m,
            },
            SenderInformation = new RemotePrintCore.Web.Models.ViewModels.SenderInformationViewModel
            {
                FullName = "Дистрибуция АД",
                Adrress = "ул. Складова 5, София 1592",
            },
        },
        DocumentItems =
        [
            new RemotePrintCore.Web.Models.ViewModels.DocumentItemViewModel
            {
                CodeNumber = "ART-001",
                Description = "Минерална вода 0.5л",
                Quantity = 24,
                UnitDemension = "бр",
                RetailPrice = 0.80,
                SalesPrice = 0.65,
                TotalSalesPrice = 15.60,
                PaletNumber = "П-01",
            },
            new RemotePrintCore.Web.Models.ViewModels.DocumentItemViewModel
            {
                CodeNumber = "ART-002",
                Description = "Сок портокал 1л",
                Quantity = 12,
                UnitDemension = "бр",
                RetailPrice = 2.50,
                SalesPrice = 2.10,
                TotalSalesPrice = 25.20,
                PaletNumber = "П-01",
            },
            new RemotePrintCore.Web.Models.ViewModels.DocumentItemViewModel
            {
                CodeNumber = "ART-003",
                Description = "Хляб пшеничен 500г",
                Quantity = 50,
                UnitDemension = "бр",
                RetailPrice = 1.40,
                SalesPrice = 1.20,
                TotalSalesPrice = 60.00,
                PaletNumber = "П-02",
            },
        ],
        DocumentFooter = new RemotePrintCore.Web.Models.ViewModels.DocumentFooterViewModel
        {
            TotalSalesPrice = 100.80,
            AuthorName = "Петър Петров",
        },
    };

    var html = await viewRenderer.RenderToStringAsync($"~/Views/Export/{template}.cshtml", model);
    return Results.Content(html, "text/html");
}).RequireAuthorization();

// Blazor
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
