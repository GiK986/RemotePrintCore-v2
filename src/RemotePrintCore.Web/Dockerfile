# ── Stage 1: Build ──────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/RemotePrintCore.Web/RemotePrintCore.Web.csproj src/RemotePrintCore.Web/
RUN dotnet restore src/RemotePrintCore.Web/RemotePrintCore.Web.csproj

COPY . .
RUN dotnet publish src/RemotePrintCore.Web/RemotePrintCore.Web.csproj \
    -c Release -o /app/publish --no-restore

# Install Playwright CLI and download Chromium into /ms-playwright
ENV PLAYWRIGHT_BROWSERS_PATH=/ms-playwright
RUN dotnet tool install --global Microsoft.Playwright.CLI \
    && cd src/RemotePrintCore.Web \
    && /root/.dotnet/tools/playwright install --with-deps chromium

# ── Stage 2: Runtime ─────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Chromium system dependencies (Ubuntu/Debian)
RUN apt-get update && apt-get install -y --no-install-recommends \
    libnss3 libnspr4 libdbus-1-3 libatk1.0-0 libatk-bridge2.0-0 \
    libcups2 libdrm2 libxkbcommon0 libxcomposite1 libxdamage1 \
    libxfixes3 libxrandr2 libgbm1 libpango-1.0-0 libcairo2 \
    libasound2t64 libx11-6 libxcb1 libxext6 libxshmfence1 \
    fonts-liberation libglib2.0-0 \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .
COPY --from=build /ms-playwright /ms-playwright

RUN mkdir -p /app/data /app/wwwroot/uploads /app/logs

ENV PLAYWRIGHT_BROWSERS_PATH=/ms-playwright \
    ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_NOLOGO=1

EXPOSE 8080
ENTRYPOINT ["dotnet", "RemotePrintCore.Web.dll"]
