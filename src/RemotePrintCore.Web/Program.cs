using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using RemotePrintCore.Web.Components;
using RemotePrintCore.Web.Data;
using RemotePrintCore.Web.Models.Entities;
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
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
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

// Blazor
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
