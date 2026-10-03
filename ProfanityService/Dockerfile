FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Monitoring/Monitoring.csproj Monitoring/
COPY ProfanityService/ProfanityService.csproj ProfanityService/

RUN dotnet restore ProfanityService/ProfanityService.csproj

COPY . .

RUN dotnet publish ProfanityService/ProfanityService.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "ProfanityService.dll"]