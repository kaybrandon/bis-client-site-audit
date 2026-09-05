FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY src/BisAudit.Api/BisAudit.Api.csproj src/BisAudit.Api/
RUN dotnet restore src/BisAudit.Api/BisAudit.Api.csproj
COPY src/BisAudit.Api/ src/BisAudit.Api/
RUN dotnet publish src/BisAudit.Api/BisAudit.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
RUN mkdir -p /app/wwwroot/uploads
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "BisAudit.Api.dll"]
