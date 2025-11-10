FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project file and restore
COPY ["Paperless.OcrWorker.csproj", "./"]
RUN dotnet restore "Paperless.OcrWorker.csproj"

# Copy the rest and publish
COPY . .
RUN dotnet publish "Paperless.OcrWorker.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Paperless.OcrWorker.dll"]
