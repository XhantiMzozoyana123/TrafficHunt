# Running TrafficHunt in Docker

This folder contains everything needed to run the TrafficHunt web app in a Docker container on your VPS.

## Files

| File | Purpose |
|---|---|
| `Dockerfile` | Multi-stage build (SDK → publish → aspnet runtime). |
| `docker-compose.yml` | Full stack: `web` app + optional `db` (MySQL). |
| `.env.example` | Template for your environment/secrets. **Copy to `.env` and fill in.** |
| `config/appsettings.json` | Runtime config the app mounts + **persists OAuth tokens back into**. |
| `.dockerignore` | Keeps `bin/`, `obj/`, logs, and secrets out of the image. |

## How it works

- **Multi-stage Dockerfile**: compiles in the .NET 10 SDK image, runs in the lightweight `aspnet:10.0` runtime.
- **Config via appsettings + env**: the container mounts `config/appsettings.json` at `/app/appsettings.json` **read-write**. Because the app **rewrites this file at runtime** to store refreshed Google OAuth tokens, the mount is required for persistent OAuth (a `:ro` mount or no mount would lose your Google connection on restart / crash token refresh).
- **MySQL**: two options, see below.
- **Auto migrations**: `AutoMigrate=true` in `.env` makes the app apply EF migrations on startup, so a brand-new empty database is set up automatically — no manual `dotnet ef` needed.

## Quick start on your VPS

1. **Install Docker** (Debian/Ubuntu): `curl -fsSL https://get.docker.com | sh`
2. Clone/copy the repo to the VPS.
3. Create your config + env:
   ```bash
   cp .env.example .env        # then edit values (passwords, Google OAuth, Ollama)
   mkdir -p config
   cp config/appsettings.json.example config/appsettings.json 2>/dev/null || true
   # Edit config/appsettings.json with your Ollama URL + any starter values.
   ```
4. Build & start:
   ```bash
   docker compose up -d --build
   ```
5. Check it: `docker compose ps`, then open `http://VPS_IP:8080`.

## Choosing the database setup

**Option A — MySQL runs as a container (simplest):**
- Keep the `db` service in `docker-compose.yml`, leave `DB_HOST=db` in `.env`.
- The `web` service depends on `db` and `AutoMigrate=true` creates the schema.

**Option B — use an existing/external MySQL:**
- Delete (or comment out) the `db` service in `docker-compose.yml`.
- Set `DB_HOST` to your MySQL host/IP, and `DB_USER`/`DB_PASSWORD` accordingly.
- Keep `AutoMigrate=true` so the schema is created in your database.

## Important notes

- **Persist settings**: `appsettings.json` is mounted **read-write** so the app can store Google OAuth tokens (it rewrites the file on token exchange/refresh). Don't add `:ro` to the mount or OAuth will fail.
- **HTTPS**: the container runs plain HTTP on port 80 and `DisableHttpsRedirect=true` is set — put a reverse proxy (Caddy/Nginx/Traefik + Let's Encrypt) in front if you want public HTTPS.
- **PublishPort**: the host port is controlled by `PublishPort` (default 8080).
- **Secrets**: `.env` and `config/appsettings.json` are git-ignored. Never commit real credentials.

## Manual (no compose)

```bash
docker build -t traffichunt:latest .
docker run -d --name traffichunt -p 8080:80 \
  -e ASPNETCORE_URLS=http://+:80 \
  -e DisableHttpsRedirect=true \
  -e AutoMigrate=true \
  -e ConnectionStrings__DefaultConnection="Server=db;Port=3306;Database=traffichunt;Uid=traffichunt;Pwd=traffichunt_secret" \
  -e GoogleAuth__ClientId="YOUR_CLIENT_ID" \
  -e GoogleAuth__ClientSecret="YOUR_CLIENT_SECRET" \
  -e Ollama__BaseUrl="https://llm.processzero.xyz" \
  -e Ollama__Model="llama3" \
  -v /absolute/path/config/appsettings.json:/app/appsettings.json \
  traffichunt:latest
```