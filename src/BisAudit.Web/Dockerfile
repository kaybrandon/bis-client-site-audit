FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY src/BisAudit.Web/BisAudit.Web.csproj src/BisAudit.Web/
RUN dotnet restore src/BisAudit.Web/BisAudit.Web.csproj
COPY src/BisAudit.Web/ src/BisAudit.Web/
RUN dotnet publish src/BisAudit.Web/BisAudit.Web.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
RUN mkdir -p /app/wwwroot/uploads
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "BisAudit.Web.dll"]
