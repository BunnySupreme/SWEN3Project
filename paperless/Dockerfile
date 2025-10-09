FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src
COPY ["paperless.csproj", "."]
RUN dotnet restore "paperless.csproj"
COPY . .
RUN dotnet build "paperless.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "paperless.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "paperless.dll"]