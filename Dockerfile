# WinCleaner Dockerfile - Multi-stage build for CI
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY WinCleaner.sln .
COPY Directory.Build.props .
COPY Directory.Packages.props .
COPY global.json .
COPY Version.props .
COPY version.json .

# Copy source
COPY src/ src/
COPY tests/ tests/
COPY WinCleaner-classic/ WinCleaner-classic/

# Restore
RUN dotnet restore WinCleaner.sln

# Build
RUN dotnet build WinCleaner.sln --configuration Release --no-restore

# Test
RUN dotnet test WinCleaner.sln --configuration Release --no-build --logger "console;verbosity=detailed"

# Publish Modern App
RUN dotnet publish src/WinCleaner.App/WinCleaner.App.csproj -c Release -o /app/modern --self-contained true -r linux-x64

# Publish CLI
RUN dotnet publish src/WinCleaner.Cli/WinCleaner.Cli.csproj -c Release -o /app/cli --self-contained true -r linux-x64

# Runtime image
FROM mcr.microsoft.com/dotnet/runtime-deps:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/modern ./modern
COPY --from=build /app/cli ./cli

# Non-root user
RUN useradd -m -u 1000 wincleaner && chown -R wincleaner:wincleaner /app
USER wincleaner

ENTRYPOINT ["./modern/WinCleaner.App"]