FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
WORKDIR /src

COPY paperless/src/paperless/paperless.csproj paperless/src/paperless/
RUN dotnet restore "paperless/src/paperless/paperless.csproj"

COPY paperless/src/paperless/ paperless/src/paperless/
WORKDIR /src/paperless/src/paperless
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "paperless.dll"]
