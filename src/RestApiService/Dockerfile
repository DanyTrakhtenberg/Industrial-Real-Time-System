FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY src/Shared.Contracts/Shared.Contracts.csproj src/Shared.Contracts/
COPY src/RestApiService/RestApiService.csproj src/RestApiService/
RUN dotnet restore src/RestApiService/RestApiService.csproj
COPY src/Shared.Contracts/ src/Shared.Contracts/
COPY src/RestApiService/ src/RestApiService/
RUN dotnet publish src/RestApiService/RestApiService.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "RestApiService.dll"]
