# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
WORKDIR /src
COPY ["paperless.csproj", "."]
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Deploy stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS deploy
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "paperless.dll"]