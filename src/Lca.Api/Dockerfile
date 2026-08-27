FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props global.json Lca.slnx ./
COPY src/Lca.Core/Lca.Core.csproj src/Lca.Core/
COPY src/Lca.Infrastructure/Lca.Infrastructure.csproj src/Lca.Infrastructure/
COPY src/Lca.Api/Lca.Api.csproj src/Lca.Api/
RUN dotnet restore src/Lca.Api/Lca.Api.csproj

COPY src/ src/
RUN dotnet publish src/Lca.Api/Lca.Api.csproj --no-restore --configuration Release --output /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_EnableDiagnostics=0

COPY --from=build /app/publish ./
USER $APP_UID
EXPOSE 8080
HEALTHCHECK --interval=10s --timeout=3s --start-period=10s --retries=5 CMD ["dotnet", "Lca.Api.dll", "--health-check"]
ENTRYPOINT ["dotnet", "Lca.Api.dll"]
