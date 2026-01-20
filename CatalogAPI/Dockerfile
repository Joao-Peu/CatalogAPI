FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies (melhor cache)
COPY ["CatalogAPI.csproj", "./"]
RUN dotnet restore "CatalogAPI.csproj"

# Copy everything else and build
COPY . .
RUN dotnet build "CatalogAPI.csproj" -c Release -o /app/build

# Publish Stage
FROM build AS publish
RUN dotnet publish "CatalogAPI.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Copy published files first (as root)
COPY --from=publish /app/publish .

# Environment configuration - Use port 8080 (non-privileged)
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

EXPOSE 8080

# Security: Run as non-root user (AFTER copying files)
USER app

ENTRYPOINT ["dotnet", "CatalogAPI.dll"]
