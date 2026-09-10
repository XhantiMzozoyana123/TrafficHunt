# TrafficHunt — ASP.NET Core MVC app (net10.0)
# Multi-stage build: SDK image compiles/publishes, runtime image runs it.

# ---- Build stage ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore as a separate layer (better caching when deps change).
# Copy all projects so `dotnet restore` can resolve the full project graph
# (the Web project references Application + Infrastructure; a slnx also exists).
COPY . .
RUN dotnet restore "TrafficHunt.Web/TrafficHunt.Web.csproj"

# Publish (Release, self-contained=false — uses shared framework in runtime image).
RUN dotnet publish "TrafficHunt.Web/TrafficHunt.Web.csproj" \
    -c Release -o /app/publish \
    --no-restore

# ---- Runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# wget is needed for the compose healthcheck (not present in the minimal runtime image).
USER root
RUN apt-get update \
    && apt-get install -y --no-install-recommends wget \
    && rm -rf /var/lib/apt/lists/*
# Pre-create an empty appsettings.json owned by the app user so the
# read-write mount works even if the host file is root-owned/missing.
RUN touch /app/appsettings.json && chown $APP_UID /app/appsettings.json

# ASP.NET Core listens on port 80 by default in the runtime image.
EXPOSE 80

# Non-root user recommended by Microsoft; safer in production.
# NOTE: $APP_UID is defined by the base image — only usable after it is set,
# so the non-root switch happens here at the end (after the root steps above).
USER $APP_UID

# The app reads config from appsettings.json + environment variables.
# Secrets (Google OAuth, DB password) should come in via env vars or a mounted
# appsettings.json volume — do NOT bake them into the image.
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "TrafficHunt.Web.dll"]