# RemotePrintCore v2

Система за отдалечен печат на документи (фактури, складови разписки) чрез SOAP услуга. Получава XML данни, генерира PDF с Playwright Chromium и изпраща към мрежов принтер по TCP/IP.

## Стек

- **ASP.NET Core 10** + Blazor Server (UI) + SOAP (asmx-style endpoint)
- **Playwright Chromium** — рендериране на PDF
- **SQLite** — локална база данни
- **Serilog** — структурирано логване (конзола + файл)
- **Traefik** — reverse proxy + TLS (Let's Encrypt)

## Функционалности

- SOAP endpoint за получаване на документи и иницииране на печат
- Поддръжка на множество принтери (IP:Port)
- Банери — изображения, вграждани в PDF (drag & drop управление)
- Шаблони за документи (различен layout per template)
- Дневник на печатите с търсене и филтриране
- Известия при грешка — Email, Telegram, Viber
- Уеб администрация с вход (ASP.NET Core Identity)

## Локална разработка

```bash
# 1. Конфигурация (gitignored)
cp .env.example appsettings.Development.json
# Попълни SMTP и др. настройки в appsettings.Development.json

# 2. Инсталирай Playwright Chromium
dotnet tool install --global Microsoft.Playwright.CLI
playwright install chromium

# 3. Стартирай
dotnet run --project src/RemotePrintCore.Web
```

## Деплоймент (Docker)

```bash
# Копирай и попълни .env
cp .env.example .env

# Билдвай и стартирай
docker compose up -d
```

Изисква съществуваща Traefik `proxy` мрежа с `letsencrypt` certresolver.

## Конфигурация

Всички настройки се подават чрез `.env` (за Docker) или `appsettings.Development.json` (локално).
Вижте `.env.example` за пълния списък с променливи.

## SOAP Endpoint

```
https://localhost/Service.asmx
```

WSDL: `https://localhost/Service.asmx?wsdl`
