FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Monitoring/Monitoring.csproj Monitoring/
COPY Contracts/Contracts.csproj Contracts/
COPY ArticleService/ArticleService.csproj ArticleService/

RUN dotnet restore ArticleService/ArticleService.csproj

COPY . .

RUN dotnet publish ArticleService/ArticleService.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "ArticleService.dll"]