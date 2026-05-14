FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY src/Shared.Contracts/Shared.Contracts.csproj src/Shared.Contracts/
COPY src/SqlDataService/SqlDataService.csproj src/SqlDataService/
RUN dotnet restore src/SqlDataService/SqlDataService.csproj
COPY src/Shared.Contracts/ src/Shared.Contracts/
COPY src/SqlDataService/ src/SqlDataService/
RUN dotnet publish src/SqlDataService/SqlDataService.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "SqlDataService.dll"]
