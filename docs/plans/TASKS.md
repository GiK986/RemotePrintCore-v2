# RemotePrintCore v2 — Task Progress

> Full implementation plan: `docs/plans/2026-03-15-implementation-plan.md`

## Phase 0: Repository Setup

- [x] Task 0: Initialize git repo + connect to GitHub

## Phase 1: Foundation

- [x] Task 1: Create .NET 10 project + solution file + NuGet packages
- [x] Task 2: Entity models + DbContext + SQLite (migration applied)
- [x] Task 3: Database seeding (roles + default admin)
- [x] Task 4: SOAP data contracts (DTOs)
- [x] Task 5: View models + Razor templates (copy from v1, fix URLs)
- [x] Task 6: Manual mapping helper (DocumentInfo → ViewModel)

## Phase 2: Core Services

- [x] Task 7: ViewRenderService (Razor → HTML string)
- [x] Task 8: PlaywrightPdfConverter (HTML → PDF bytes)
- [x] Task 9: DocumentGenerator (orchestrates 7+8)
- [x] Task 10: TcpPrinterService (IP:Port printing)
- [x] Task 11: BannerService
- [x] Task 12: DocumentTemplatesService
- [x] Task 13: PrintLogService
- [x] Task 14: NotificationDispatcher (Email, Telegram, Viber)
- [x] Task 15: SOAP Service endpoint (full orchestration)

## Phase 3: Web UI

- [x] Task 16: Blazor infrastructure + layout (App, Routes, MainLayout, NavMenu)
- [x] Task 17: Login page
- [x] Task 18: Dashboard page
- [x] Task 19: Printers CRUD page
- [x] Task 20: Banners management page (drag & drop)
- [x] Task 21: Document Templates page
- [x] Task 22: Print Log page

## Phase 4: Docker

- [ ] Task 23: Dockerfile (multi-stage, Playwright Chromium)
- [ ] Task 24: docker-compose.yml (Traefik labels)

## Phase 5: Polish

- [ ] Task 25: Serilog structured logging
- [ ] Task 26: End-to-end integration test
