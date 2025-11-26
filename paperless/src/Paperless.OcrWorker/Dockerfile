# ───────────────────────────────
# BASE IMAGE (Debian Bookworm)
# ───────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:9.0-bookworm-slim AS base
WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends \
       ghostscript \
       libleptonica-dev \
       libtesseract-dev \
    && rm -rf /var/lib/apt/lists/*

ENV TESSDATA_PREFIX=/app/tessdata

RUN ln -s /usr/lib/x86_64-linux-gnu/libdl.so.2 /usr/lib/x86_64-linux-gnu/libdl.so

WORKDIR /app/x64
RUN ln -s /usr/lib/x86_64-linux-gnu/liblept.so.5       /app/x64/libleptonica-1.82.0.so \
 && ln -s /usr/lib/x86_64-linux-gnu/libtesseract.so.5  /app/x64/libtesseract50.so

WORKDIR /app

# ───────────────────────────────
# BUILD STAGE
# ───────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:9.0-bookworm-slim AS build
WORKDIR /src

COPY ["Paperless.OcrWorker.csproj", "./"]
RUN dotnet restore "./Paperless.OcrWorker.csproj"

COPY . .
RUN dotnet publish "Paperless.OcrWorker.csproj" -c Release -o /app/publish /p:UseAppHost=false

# ───────────────────────────────
# FINAL STAGE
# ───────────────────────────────
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Paperless.OcrWorker.dll"]
