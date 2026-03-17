# RemotePrintCore v2 — Архитектурен дизайн

**Дата:** 2026-03-15
**Статус:** Одобрен
**Референция:** `/Users/gik986/Developer/RemotePrintCore` (стария проект)

---

## Контекст

Модернизация на съществуващ ASP.NET Core 2.2 / Windows / IIS проект за отдалечен печат.
Целта е преход към .NET 10.0/ Linux / Docker без промяна на SOAP интерфейса към клиентите (ERP системата).

---

## Стек

| Компонент        | Технология                        |
|------------------|-----------------------------------|
| Runtime          | .NET 10.0/ Linux                    |
| SOAP             | SoapCore NuGet пакет              |
| PDF генерация    | Playwright (Chromium headless)    |
| Печат            | `TcpClient` → IP:Port             |
| База данни       | SQLite + EF Core                  |
| Web UI           | Blazor Server + MudBlazor         |
| Автентикация     | ASP.NET Core Identity             |
| Container        | Docker + Traefik (HTTPS external) |

---

## SOAP Контракт

Запазва се пълна backward compatibility. Единствената промяна е добавянето на опционален трети параметър:

```
PrintDocumentInfo(DocumentInfo, PrinterOption, NotificationOptions?)
GetAllInstalledPrinters()
GetAllDocumentTemplateNames()
Test(string)
```

### NotificationOptions (нов, nullable)
```csharp
public class NotificationOptions
{
    public string? Email { get; set; }
    public string? TelegramChatId { get; set; }
    public string? ViberUserId { get; set; }
}
```

- Конфигурацията се пази в ERP системата на ниво клиентска карта
- ERP подава само попълнените канали — останалите са `null`
- Съществуващи клиенти без третия параметър работят без промяна

---

## Поток на документ

```
ERP → SOAP call: PrintDocumentInfo(DocumentInfo, PrinterOption, NotificationOptions?)
         │
         ▼
  Resolve PrinterName → IP:Port
  (справка в SQLite таблица Printers)
         │
         ▼
  Razor View → HTML string
  (шаблонът се рендира с данните от DocumentInfo)
         │
         ▼
  Playwright → PDF bytes
  (Chromium headless, base URL = http://localhost)
         │
         ├──────────────────────────────┐
         ▼                              ▼
  TcpClient.Write(pdfBytes)    NotificationDispatcher
  към принтера IP:Port         (паралелно, неблокиращо)
  [блокиращо — чака потвърждение]   │
                                    ├── Email (MailKit + PDF attachment)
                                    ├── Telegram (Telegram.Bot)
                                    └── Viber (HttpClient REST API)
```

**Важно:** Грешка в нотификация не блокира печата и не връща грешка към ERP.

---

## База данни (SQLite)

```
Printers
  Id, Name, IpAddress, Port, IsActive, CreatedOn, ModifiedOn

Banners                          ← замества "Settings" от v1
  Id, Name, FileName, FromDate, ToDate, CreatedOn, ModifiedOn

DocumentTemplates
  Id, Name, Values, CreatedOn, ModifiedOn

PrintLog
  Id, DocumentNumber, CustomerName, PrinterName, TemplateName,
  NumberOfCopies, Status, ErrorMessage, CreatedOn
```

---

## Принтери — управление

`PrinterName` в `PrinterOption` остава непроменен (ERP не знае за IP адреси).
Сервизът пази mapping `PrinterName → IP:Port` в таблица `Printers`, управлявана през Web UI.

---

## PDF генерация

- Razor шаблоните се запазват почти без промяна
- `HtmlToPdfConverter` (wkhtmltopdf) → заменя се с Playwright
- Base URL е конфигурируем в `appsettings.json` (`http://localhost` в Docker)
- Ресурси (CSS, изображения, банери) се сервират от същото приложение

---

## Web UI (Blazor Server + MudBlazor)

### Роли
| Роля            | Достъп                              |
|-----------------|-------------------------------------|
| `Admin`         | Всичко                              |
| `BannerManager` | Само банери                         |

### Страници

#### Dashboard
- Активни банери (брой и thumbnails)
- Принтери с online/offline индикация
- Последни 10 записа от PrintLog

#### Банери
- Drag & drop upload с image preview
- Поле за Name, FromDate, ToDate
- Таблица с активни/неактивни банери
- Изтриване / деактивиране

#### Принтери
- CRUD таблица: Name · IP Address · Port · Active
- Тест бутон (ping към IP:Port)

#### Шаблони
- Списък на наличните Razor шаблони
- Управление на `DocumentTemplates` (name → values mapping)

#### Лог
- История на отпечатани документи
- Филтри по дата, принтер, шаблон, статус

---

## Docker

```yaml
services:
  app:
    image: remoteprintcore-v2
    # Traefik labels за HTTPS
    volumes:
      - sqlite_data:/app/data
      - uploads:/app/wwwroot/uploads

volumes:
  sqlite_data:
  uploads:
```

- Traefik е external (вече наличен)
- Един контейнер — без отделен DB сървър
- Playwright Chromium се включва в Docker image

---

## Премахнати зависимости от v1

| v1                          | v2                        |
|-----------------------------|---------------------------|
| Win32 P/Invoke (Spooler)    | `TcpClient`               |
| wkhtmltopdf                 | Playwright                |
| SoapCore (vendored код)     | SoapCore NuGet пакет      |
| SQL Server                  | SQLite                    |
| `Settings` entity (банери)  | `Banners` entity          |
| Windows path `\\`           | `Path.Combine()`          |
| External URL за ресурси     | `http://localhost`        |
| IIS / Windows Server        | Docker / Linux            |

---

## Следваща стъпка

Имплементационен план (writing-plans).
