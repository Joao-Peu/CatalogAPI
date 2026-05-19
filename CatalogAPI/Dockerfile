FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies (cache layer)
COPY ["CatalogAPI/CatalogAPI.csproj", "CatalogAPI/"]
RUN dotnet restore "CatalogAPI/CatalogAPI.csproj"

# Copy everything else
COPY CatalogAPI/. CatalogAPI/
WORKDIR /src/CatalogAPI

# Build
RUN dotnet build "CatalogAPI.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "CatalogAPI.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=publish /app/publish .

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

EXPOSE 8080

# (opcional mas recomendado: cria usuário se não existir)
USER app

ENTRYPOINT ["dotnet", "CatalogAPI.dll"]
