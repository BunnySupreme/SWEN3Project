FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
WORKDIR /src

COPY src/paperless/paperless.csproj src/paperless/
RUN dotnet restore "src/paperless/paperless.csproj"

COPY . .

RUN dotnet publish "src/paperless/paperless.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "paperless.dll"]
