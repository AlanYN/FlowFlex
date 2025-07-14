# Use the official .NET 8.0 SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

# Set the working directory
WORKDIR /app

# Copy local packages first - this is crucial for local dependencies
COPY packages/ ./packages/

# Create Docker-specific nuget.config that only uses local packages and nuget.org
RUN echo '<?xml version="1.0" encoding="utf-8"?>' > nuget.config && \
    echo '<configuration>' >> nuget.config && \
    echo '  <packageSources>' >> nuget.config && \
    echo '    <clear />' >> nuget.config && \
    echo '    <add key="local-packages" value="./packages" />' >> nuget.config && \
    echo '    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />' >> nuget.config && \
    echo '  </packageSources>' >> nuget.config && \
    echo '  <config>' >> nuget.config && \
    echo '    <add key="globalPackagesFolder" value="./packages" />' >> nuget.config && \
    echo '  </config>' >> nuget.config && \
    echo '</configuration>' >> nuget.config

# Copy solution and project files for dependency resolution
COPY *.sln ./
COPY Directory.Build.props ./

# Copy all project files
COPY Application/*.csproj ./Application/
COPY Application.Contracts/*.csproj ./Application.Contracts/
COPY Domain/*.csproj ./Domain/
COPY Domain.Shared/*.csproj ./Domain.Shared/
COPY Infrastructure/*.csproj ./Infrastructure/
COPY SqlSugarDB/*.csproj ./SqlSugarDB/
COPY WebApi/*.csproj ./WebApi/

# Set environment variables to avoid Windows-specific paths
ENV NUGET_PACKAGES=/app/packages
ENV NUGET_FALLBACK_PACKAGES=""

# Restore dependencies - use local packages first, then fallback to nuget.org
RUN dotnet restore --configfile nuget.config --verbosity detailed

# Copy the entire source code
COPY . .

# Clean any existing build artifacts but keep obj folders with assets
RUN find . -name "bin" -type d -exec rm -rf {} + 2>/dev/null || true

# Restore again after copying all source code to regenerate project.assets.json
RUN dotnet restore --configfile nuget.config --verbosity minimal

# Build the application with explicit package folder
RUN dotnet build -c Release --no-restore --verbosity minimal -p:RestorePackagesPath=/app/packages -p:NuGetPackageRoot=/app/packages

# Publish the application
RUN dotnet publish WebApi/WebApi.csproj -c Release -o /app/publish --no-restore --verbosity minimal -p:RestorePackagesPath=/app/packages -p:NuGetPackageRoot=/app/packages

# Use the official .NET 8.0 runtime image for the final stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime

# Set the working directory
WORKDIR /app

# Create a non-root user for security
RUN groupadd --gid 1001 flowflex && \
    useradd --uid 1001 --gid 1001 --create-home --shell /bin/bash flowflex

# Install required packages for the application
RUN apt-get update && apt-get install -y \
    curl \
    && rm -rf /var/lib/apt/lists/*

# Copy the published application from the build stage
COPY --from=build /app/publish .

# Create directories for file storage and logs
RUN mkdir -p /app/wwwroot/uploads /app/logs && \
    chown -R flowflex:flowflex /app

# Switch to non-root user
USER flowflex

# Expose the port the app runs on
EXPOSE 8080

# Set environment variables
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# Set the entry point
ENTRYPOINT ["dotnet", "WebApi.dll"] 