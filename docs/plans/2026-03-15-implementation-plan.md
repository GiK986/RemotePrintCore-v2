# RemotePrintCore v2 Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Modernize RemotePrintCore from ASP.NET Core 2.2/Windows/IIS to .NET 8/Linux/Docker with Blazor UI, TCP printing, and notification channels.

**Architecture:** Single .NET 10.0 project with SoapCore for SOAP endpoints, Playwright for PDF generation, TcpClient for printing, Blazor Server + MudBlazor for Web UI, SQLite for data, Docker for deployment behind existing Traefik reverse proxy.

**Tech Stack:** .NET 8, SoapCore NuGet, Playwright, EF Core + SQLite, Blazor Server, MudBlazor, MailKit, Telegram.Bot, Docker

**Reference project:** `/Users/gik986/Developer/RemotePrintCore`
**New project:** `/Users/gik986/Developer/RemotePrintCore-v2`
**GitHub repo:** `git@github.com:GiK986/RemotePrintCore-v2.git`
**Architecture doc:** `/Users/gik986/Developer/RemotePrintCore-v2/docs/plans/2026-03-15-architecture-design.md`

---

## Project Structure

```
RemotePrintCore-v2/
├── docs/plans/
├── src/RemotePrintCore.Web/
│   ├── RemotePrintCore.Web.csproj
│   ├── Program.cs
│   ├── appsettings.json
│   ├── Models/
│   │   ├── Entities/        (Printer, Banner, DocumentTemplate, PrintLog, BaseEntity)
│   │   ├── Soap/            (DocumentInfo, PrinterOption, NotificationOptions — DTOs)
│   │   └── ViewModels/      (DocumentInfoViewModel and children)
│   ├── Data/
│   │   ├── AppDbContext.cs
│   │   └── Seeding/
│   ├── Services/
│   │   ├── Pdf/             (ViewRenderService, PlaywrightPdfConverter, DocumentGenerator)
│   │   ├── Printing/        (TcpPrinterService)
│   │   ├── Banners/         (BannerService)
│   │   ├── Templates/       (DocumentTemplatesService)
│   │   ├── Notifications/   (NotificationDispatcher + channels)
│   │   ├── PrintLog/        (PrintLogService)
│   │   ├── Mapping/         (DocumentMapper — manual static mapper)
│   │   └── Soap/            (ISampleService, SampleService)
│   ├── Views/
│   │   ├── Export/          (5 Razor templates — copied from v1)
│   │   └── Shared/          (Layouts)
│   ├── Components/
│   │   ├── App.razor
│   │   ├── Routes.razor
│   │   ├── Layout/          (MainLayout, NavMenu)
│   │   └── Pages/           (Dashboard, Printers, Banners, Templates, PrintLog, Login)
│   ├── wwwroot/
│   │   ├── css/template.css
│   │   ├── image/           (logo)
│   │   └── uploads/         (banner images — Docker volume)
│   ├── Dockerfile
│   └── docker-compose.yml
```

---

## Phase 0: Repository Setup

### Task 0: Initialize git repo + connect to GitHub

**Step 1:** Initialize git in project root
```bash
cd /Users/gik986/Developer/RemotePrintCore-v2
git init
```

**Step 2:** Create `.gitignore` for .NET
```bash
dotnet new gitignore
```

**Step 3:** Add remote and initial commit
```bash
git remote add origin git@github.com:GiK986/RemotePrintCore-v2.git
git add .
git commit -m "Initial commit: architecture design and implementation plan"
git branch -M main
git push -u origin main
```

**Verify:** `git remote -v` shows GitHub remote. Docs visible on GitHub.

---

## Phase 1: Foundation

### Task 1: Create .NET 10.0project + NuGet packages

**Files:**
- Create: `src/RemotePrintCore.Web/RemotePrintCore.Web.csproj`
- Create: `src/RemotePrintCore.Web/Program.cs` (minimal hosting skeleton)
- Create: `src/RemotePrintCore.Web/appsettings.json`
- Create: `src/RemotePrintCore.Web/appsettings.Development.json`

**Step 1:** Create project
```bash
cd /Users/gik986/Developer/RemotePrintCore-v2
mkdir -p src/RemotePrintCore.Web
cd src/RemotePrintCore.Web
dotnet new web -n RemotePrintCore.Web --no-https
```

**Step 2:** Add NuGet packages
```bash
dotnet add package SoapCore
dotnet add package Microsoft.EntityFrameworkCore.Sqlite
dotnet add package Microsoft.EntityFrameworkCore.Design
dotnet add package Microsoft.AspNetCore.Identity.EntityFrameworkCore
dotnet add package Microsoft.Playwright
dotnet add package MudBlazor
dotnet add package MailKit
dotnet add package Telegram.Bot
dotnet add package Serilog.AspNetCore
dotnet add package Serilog.Sinks.Console
dotnet add package Serilog.Sinks.File
```

**Step 3:** Write minimal `Program.cs` with builder, add MVC (for Razor views), Blazor Server, static files
**Step 4:** Write `appsettings.json` with SQLite connection string, PDF BaseUrl, notification config sections
**Step 5:** `dotnet build` — verify success

---

### Task 2: Entity models + DbContext + SQLite

**Files:**
- Create: `Models/Entities/BaseEntity.cs`
- Create: `Models/Entities/Printer.cs`
- Create: `Models/Entities/Banner.cs`
- Create: `Models/Entities/DocumentTemplate.cs`
- Create: `Models/Entities/PrintLog.cs`
- Create: `Models/Entities/ApplicationUser.cs`
- Create: `Models/Entities/ApplicationRole.cs`
- Create: `Data/AppDbContext.cs`

**BaseEntity pattern** (simplified from v1 `BaseDeletableModel<int>`):
```csharp
public abstract class BaseEntity
{
    public int Id { get; set; }
    public DateTime CreatedOn { get; set; }
    public DateTime? ModifiedOn { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedOn { get; set; }
}
```

**Printer** (NEW — replaces Win32 printer resolution):
```csharp
public class Printer : BaseEntity
{
    [Required, MaxLength(100)]
    public string Name { get; set; }
    [Required, MaxLength(45)]
    public string IpAddress { get; set; }
    public int Port { get; set; } = 9100;
    public bool IsActive { get; set; } = true;
}
```

**Banner** (replaces v1 `Setting`):
```csharp
public class Banner : BaseEntity
{
    [Required, MaxLength(30)]
    public string Name { get; set; }
    [Required, MaxLength(60)]
    public string FileName { get; set; }
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
}
```

**DocumentTemplate** (same as v1):
```csharp
public class DocumentTemplate : BaseEntity
{
    [Required, MaxLength(30)]
    public string Name { get; set; }
    [Required, MaxLength(150)]
    public string Values { get; set; }
}
```

**PrintLog** (NEW):
```csharp
public class PrintLog
{
    public int Id { get; set; }
    public string DocumentNumber { get; set; }
    public string? CustomerName { get; set; }
    public string PrinterName { get; set; }
    public string TemplateName { get; set; }
    public int NumberOfCopies { get; set; }
    public PrintStatus Status { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedOn { get; set; }
}
public enum PrintStatus { Success, Error }
```

**AppDbContext:** DbSets for all entities, global query filter `IsDeleted == false` on BaseEntity subclasses, audit info in `SaveChangesAsync` override (same pattern as v1 `ApplicationDbContext.cs`).

**Verify:** `dotnet ef migrations add InitialCreate` + `dotnet ef database update`

---

### Task 3: Database seeding

**Files:**
- Create: `Data/Seeding/DbSeeder.cs`

Seed roles ("Administrator", "BannerManager") and a default admin user on first startup.

**Verify:** Run app, check SQLite has roles

---

### Task 4: SOAP data contracts (DTOs)

**Files:**
- Create: `Models/Soap/DocumentInfo.cs` — exact copy from v1
- Create: `Models/Soap/DocumentHeader.cs` — exact copy
- Create: `Models/Soap/DocumentFooter.cs` — exact copy
- Create: `Models/Soap/DocumentItem.cs` — exact copy
- Create: `Models/Soap/RecipientInformation.cs` — exact copy
- Create: `Models/Soap/SenderInformation.cs` — exact copy
- Create: `Models/Soap/PrinterOption.cs` — exact copy (PrinterName, NumberOfCopies, TemplateName)
- Create: `Models/Soap/NotificationOptions.cs` — NEW

**v1 reference files:**
- `/Users/gik986/Developer/RemotePrintCore/Services/RemotePrintCore.Services/SoapPrintService/DocumentDataContracts/*.cs`
- `/Users/gik986/Developer/RemotePrintCore/Services/RemotePrintCore.Services/SoapPrintService/PrinterOptionContracts/PrinterOption.cs`

**NotificationOptions (new):**
```csharp
[Serializable]
public class NotificationOptions
{
    public string? Email { get; set; }
    public string? TelegramChatId { get; set; }
    public string? ViberUserId { get; set; }
}
```

**Verify:** `dotnet build`

---

### Task 5: View models + Razor templates

**Files:**
- Create: `Models/ViewModels/DocumentInfoViewModel.cs` — from v1 (has `FileName`, `TotalDocumentRetailPrice` computed)
- Create: `Models/ViewModels/DocumentHeaderViewModel.cs` — from v1
- Create: `Models/ViewModels/DocumentFooterViewModel.cs` — from v1
- Create: `Models/ViewModels/DocumentItemViewModel.cs` — from v1 (has `TotalRetailPrice` computed)
- Create: `Models/ViewModels/RecipientInformationViewModel.cs` — from v1
- Create: `Models/ViewModels/SenderInformationViewModel.cs` — from v1
- Copy: `Views/Export/DocumentTemplateV1.cshtml` — update `@model` namespace
- Copy: `Views/Export/DocumentTemplateV1.1.cshtml` — update `@model` namespace
- Copy: `Views/Export/DocumentTemplateV2.cshtml` — update `@model` namespace
- Copy: `Views/Export/DocumentTemplateTR.cshtml` — update `@model` namespace
- Copy: `Views/Export/ExportPdf.cshtml` — update `@model` namespace
- Copy: `Views/Shared/_DocumentTemplateLayout.cshtml` — change URLs: `http://rrp.autoplus.bg/css/` → `/css/`, `http://rrp.autoplus.bg/image/` → `/image/` or `/uploads/`
- Copy: `Views/Shared/_ExportLayout.cshtml`
- Create: `Views/_ViewImports.cshtml`
- Copy: `wwwroot/css/template.css`
- Copy: `wwwroot/image/image000.jpg` (logo)

**v1 reference files:**
- `/Users/gik986/Developer/RemotePrintCore/Web/RemotePrintCore.Web.ViewModels/Exports/*.cs`
- `/Users/gik986/Developer/RemotePrintCore/Web/RemotePrintCore.Web/Views/Export/*.cshtml`
- `/Users/gik986/Developer/RemotePrintCore/Web/RemotePrintCore.Web/Views/Shared/_DocumentTemplateLayout.cshtml`
- `/Users/gik986/Developer/RemotePrintCore/Web/RemotePrintCore.Web/wwwroot/css/template.css`

**Key changes:** Remove `IMapFrom<>` interface (no AutoMapper), update namespaces, fix hardcoded URLs.

**Verify:** `dotnet build`

---

### Task 6: Manual mapping helper

**Files:**
- Create: `Services/Mapping/DocumentMapper.cs`

Static method `DocumentInfoViewModel Map(DocumentInfo info)` — copies all properties recursively. Replaces v1 AutoMapper.

**v1 reference:** `/Users/gik986/Developer/RemotePrintCore/Web/RemotePrintCore.Web.ViewModels/Exports/DocumentInfoViewModel.cs` — property names are identical between DTOs and ViewModels.

**Verify:** Unit test mapping a full DocumentInfo

---

## Phase 2: Core Services

### Task 7: ViewRenderService (Razor → HTML string)

**Files:**
- Create: `Services/Pdf/IViewRenderService.cs`
- Create: `Services/Pdf/ViewRenderService.cs`

Port from v1: `/Users/gik986/Developer/RemotePrintCore/Services/RemotePrintCore.Services/PDFViewService/ViewRenderService.cs`

Uses `IRazorViewEngine`, `ITempDataProvider`, `IServiceProvider`. Requires `AddControllersWithViews()` in `Program.cs`.

**Verify:** Integration test rendering a simple view

---

### Task 8: PlaywrightPdfConverter (HTML → PDF bytes)

**Files:**
- Create: `Services/Pdf/IHtmlToPdfConverter.cs`
- Create: `Services/Pdf/PlaywrightPdfConverter.cs`

Replaces v1 PhantomJS converter: `/Users/gik986/Developer/RemotePrintCore/Services/RemotePrintCore.Services/PDFViewService/HtmlToPdfConverter.cs`

**Implementation:** Singleton browser, new page per request. `Page.SetContentAsync(html)` → `Page.PdfAsync(A4, PrintBackground=true)` → return `byte[]`. No temp files.

**Verify:** Test: pass HTML, verify output starts with `%PDF`

---

### Task 9: DocumentGenerator (orchestrates view + PDF)

**Files:**
- Create: `Services/Pdf/IDocumentGenerator.cs`
- Create: `Services/Pdf/DocumentGenerator.cs`

Combines Tasks 7+8: `Task<byte[]> GenerateAsync(string templateName, object model)`

Replaces v1: `/Users/gik986/Developer/RemotePrintCore/Services/RemotePrintCore.Services/GenerateDocumentTemplate.cs`

**Verify:** Integration test generating PDF for each template

---

### Task 10: TcpPrinterService

**Files:**
- Create: `Services/Printing/IPrinterService.cs`
- Create: `Services/Printing/TcpPrinterService.cs`

Replaces v1 Win32 P/Invoke: `/Users/gik986/Developer/RemotePrintCore/Services/RemotePrintCore.Services/RawPrint/Printer.cs`

**Methods:**
- `GetAllPrinterNamesAsync()` — from DB
- `IsPrinterAvailableAsync(name)` — DB lookup
- `PrintAsync(printerName, pdfBytes, copies)` — resolve name→IP:Port from DB, `TcpClient.ConnectAsync`, write bytes × copies
- `TestConnectionAsync(ip, port)` — try connect with 3s timeout

**Verify:** Unit test with mocked DB

---

### Task 11: BannerService

**Files:**
- Create: `Services/Banners/IBannerService.cs`
- Create: `Services/Banners/BannerService.cs`

Replaces v1: `/Users/gik986/Developer/RemotePrintCore/Services/RemotePrintCore.Services.Data/SettingsService.cs`

**Methods:** `GetRandomActiveBannerFileNameAsync()`, `GetAllAsync()`, `CreateAsync(name, from, to, file)`, `DeleteAsync(id)`

**Verify:** Unit test for random selection, file upload

---

### Task 12: DocumentTemplatesService

**Files:**
- Create: `Services/Templates/IDocumentTemplatesService.cs`
- Create: `Services/Templates/DocumentTemplatesService.cs`

Direct port from v1: `/Users/gik986/Developer/RemotePrintCore/Services/RemotePrintCore.Services.Data/DocumentTemplatesService.cs`

**Verify:** Unit test

---

### Task 13: PrintLogService

**Files:**
- Create: `Services/PrintLog/IPrintLogService.cs`
- Create: `Services/PrintLog/PrintLogService.cs`

NEW — logs every print operation. Methods: `LogAsync(...)`, `GetRecentAsync(count)`, `SearchAsync(filters...)`

**Verify:** Unit test

---

### Task 14: NotificationDispatcher

**Files:**
- Create: `Services/Notifications/INotificationDispatcher.cs`
- Create: `Services/Notifications/NotificationDispatcher.cs`
- Create: `Services/Notifications/EmailNotificationChannel.cs` (MailKit)
- Create: `Services/Notifications/TelegramNotificationChannel.cs` (Telegram.Bot)
- Create: `Services/Notifications/ViberNotificationChannel.cs` (HttpClient)

NEW — parallel, non-blocking dispatch. Called fire-and-forget from SampleService. Errors logged, never thrown.

**Verify:** Unit test with mocked channels

---

### Task 15: SOAP Service endpoint

**Files:**
- Create: `Services/Soap/ISampleService.cs` — `[ServiceContract]` with 4 methods
- Create: `Services/Soap/SampleService.cs` — full orchestration

Port from v1: `/Users/gik986/Developer/RemotePrintCore/Services/RemotePrintCore.Services/SoapPrintService/SampleService.cs`

**PrintDocumentInfo flow:**
1. Resolve printer name (from `PrinterOption.PrinterName`)
2. Check printer available via `IPrinterService`
3. Map `DocumentInfo` → `DocumentInfoViewModel` via `DocumentMapper`
4. Set `viewModel.FileName` from `IBannerService.GetRandomActiveBannerFileNameAsync()`
5. Get template values from `IDocumentTemplatesService`
6. For each template name: generate PDF, print via TCP
7. Log via `IPrintLogService`
8. Fire-and-forget `INotificationDispatcher` if `notificationOptions != null`
9. Return `true`

**Program.cs registration:**
```csharp
app.UseSoapEndpoint<ISampleService>("/Service.asmx", new BasicHttpBinding(), SoapSerializer.XmlSerializer);
```

**Verify:** Call `/Service.asmx?wsdl` — WSDL returns. Call `Test("hello")` — response correct.

---

## Phase 3: Web UI (Blazor Server + MudBlazor)

### Task 16: Blazor infrastructure + layout

**Files:**
- Create: `Components/App.razor`
- Create: `Components/Routes.razor`
- Create: `Components/_Imports.razor`
- Create: `Components/Layout/MainLayout.razor` — MudBlazor layout with sidebar
- Create: `Components/Layout/NavMenu.razor` — links: Dashboard, Banners, Printers, Templates, Log

**Verify:** App starts, Blazor loads with MudBlazor theme

---

### Task 17: Login page

**Files:**
- Create: `Components/Pages/Account/Login.razor` (or scaffold Identity Razor Pages)

Simple login form. Disable public registration.

**Verify:** Login/logout works, unauthorized redirects to login

---

### Task 18: Dashboard page

**Files:**
- Create: `Components/Pages/Dashboard.razor`

Cards: active banners (thumbnails), printers (online/offline), last 10 print logs.

**Verify:** Visual verification with test data

---

### Task 19: Printers CRUD page

**Files:**
- Create: `Components/Pages/Printers.razor`

`MudTable` + `MudDialog` for add/edit. Test connection button. Soft delete.

**Verify:** CRUD persists to DB, test button works

---

### Task 20: Banners management page

**Files:**
- Create: `Components/Pages/Banners.razor`

`MudFileUpload` with drag & drop + image preview. Form: Name, FromDate, ToDate. Table of existing banners with thumbnails. Role: Admin or BannerManager.

**Verify:** Upload image, appears in table and on disk

---

### Task 21: Document Templates page

**Files:**
- Create: `Components/Pages/Templates.razor`

`MudTable` showing Name/Values. Add/edit via dialog. Admin only.

**Verify:** CRUD works

---

### Task 22: Print Log page

**Files:**
- Create: `Components/Pages/PrintLogPage.razor`

`MudTable` with server-side pagination. Filters: date range, printer, template, status.

**Verify:** Filters work correctly

---

## Phase 4: Docker

### Task 23: Dockerfile

**Files:**
- Create: `Dockerfile` (multi-stage: sdk build → aspnet runtime + Playwright Chromium deps)

Key: install Chromium dependencies in runtime image, create `/app/data` and `/app/wwwroot/uploads` directories.

**Verify:** `docker build` succeeds, `docker run` starts on port 8080

---

### Task 24: docker-compose.yml

**Files:**
- Create: `docker-compose.yml` with Traefik labels for `rrp.autoplus.bg`, volumes for sqlite_data and uploads

**Verify:** `docker compose up` starts, Traefik routes traffic

---

## Phase 5: Polish

### Task 25: Serilog structured logging

**Files:**
- Modify: `Program.cs` — Serilog sinks (Console + File)
- Modify: `Services/Soap/SampleService.cs` — structured log properties (same format as v1)

**Verify:** Logs appear with correct structured data

---

### Task 26: End-to-end integration test

**Test plan:**
1. `docker compose up`
2. GET `/Service.asmx?wsdl` → WSDL returns
3. SOAP `Test("hello")` → response
4. Add printer via Blazor UI
5. Add banner via Blazor UI
6. SOAP `GetAllInstalledPrinters()` → returns printer
7. SOAP `PrintDocumentInfo(...)` with test data → returns true
8. Check PrintLog in Blazor UI
9. Check Docker logs for structured output

---

## Verification

After all tasks complete:
1. WSDL at `/Service.asmx?wsdl` matches expected contract
2. Existing ERP client can call `PrintDocumentInfo` without changes (backward compatible)
3. PDF documents render correctly with templates
4. Printers receive PDF data via TCP
5. Blazor UI accessible with login, all CRUD works
6. Docker container runs on Linux
7. Traefik provides HTTPS
