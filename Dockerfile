FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY WebCrawler.sln ./
COPY src/WebCrawler/WebCrawler.csproj src/WebCrawler/
RUN dotnet restore src/WebCrawler/WebCrawler.csproj
COPY src/WebCrawler/ src/WebCrawler/
RUN dotnet publish src/WebCrawler/WebCrawler.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "webcrawler.dll"]
