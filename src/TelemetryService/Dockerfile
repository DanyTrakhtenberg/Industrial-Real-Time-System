FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY src/Shared.Contracts/Shared.Contracts.csproj src/Shared.Contracts/
COPY src/TelemetryService/TelemetryService.csproj src/TelemetryService/
RUN dotnet restore src/TelemetryService/TelemetryService.csproj
COPY src/Shared.Contracts/ src/Shared.Contracts/
COPY src/TelemetryService/ src/TelemetryService/
RUN dotnet publish src/TelemetryService/TelemetryService.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "TelemetryService.dll"]
