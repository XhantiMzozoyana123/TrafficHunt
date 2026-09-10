#!/bin/sh
# TrafficHunt — one-command VPS deploy.
#
# Usage (on the VPS, from the repo root):
#   chmod +x deploy.sh
#   ./deploy.sh            # build + start (creates .env / config from examples if missing)
#   ./deploy.sh --rebuild  # force a clean rebuild of the web image
#   ./deploy.sh --down     # stop the stack
#   ./deploy.sh --logs     # follow app logs
#
# What it does:
#   1. Checks docker + compose are installed.
#   2. Creates .env from .env.example (if missing) and STOPS so you can fill in secrets.
#   3. Creates config/appsettings.json from config/appsettings.json.example (if missing).
#   4. Runs `docker compose up -d --build` (or --down / --logs).
#   5. Waits for the web container to become healthy and prints status + URL.
#
# The app persists refreshed Google OAuth tokens back into the mounted
# config/appsettings.json (read-write mount) — do NOT mount it :ro.

set -eu

COMPOSE="docker compose"
REBUILD=0
ACTION="up"

for arg in "$@"; do
  case "$arg" in
    --rebuild) REBUILD=1 ;;
    --down) ACTION="down" ;;
    --logs) ACTION="logs" ;;
    -h|--help)
      echo "Usage: ./deploy.sh [--rebuild] [--down] [--logs]"
      exit 0
      ;;
    *)
      echo "Unknown option: $arg (see --help)" >&2
      exit 1
      ;;
  esac
done

log() { printf '%s\n' "$*"; }
die() { printf 'ERROR: %s\n' "$*" >&2; exit 1; }

# ---- 1. Prerequisites ----
command -v docker >/dev/null 2>&1 || die "docker is not installed. Install it first: curl -fsSL https://get.docker.com | sh"
docker compose version >/dev/null 2>&1 || die "'docker compose' plugin is missing. Update Docker to a recent version."

# ---- 2. .env bootstrap ----
if [ ! -f .env ]; then
  [ -f .env.example ] || die ".env.example not found — are you in the repo root?"
  cp .env.example .env
  log "Created .env from .env.example."
  log ">>> Edit .env now (DB passwords, GoogleAuth__ClientId/Secret, Ollama) then re-run ./deploy.sh"
  exit 0
fi

# Refuse to deploy with obviously-empty secrets.
check_secret() {
  # $1 = KEY, $2 = file
  val="$(grep -E "^$1=" "$2" | head -n 1 | cut -d= -f2- || true)"
  case "$val" in
    ""|"YOUR_"*|"changeme"|"secret"|"password")
      die "$1 in $2 looks empty/placeholder — fill it in before deploying."
      ;;
  esac
}
check_secret "GoogleAuth__ClientId" .env
check_secret "GoogleAuth__ClientSecret" .env
check_secret "DB_PASSWORD" .env
case "$(grep -E '^DB_PASSWORD=' .env | head -n 1 | cut -d= -f2- || true)" in
  "traffichunt_secret"|"traffichunt_root")
    die "DB_PASSWORD in .env is still the example placeholder — set a real password."
    ;;
esac

# ---- 3. appsettings.json bootstrap (OAuth token persistence) ----
if [ ! -f config/appsettings.json ]; then
  [ -f config/appsettings.json.example ] || die "config/appsettings.json.example not found."
  mkdir -p config
  cp config/appsettings.json.example config/appsettings.json
  log "Created config/appsettings.json from the example."
fi

# ---- Actions ----
if [ "$ACTION" = "down" ]; then
  log "Stopping TrafficHunt stack..."
  $COMPOSE down
  log "Stopped."
  exit 0
fi

if [ "$ACTION" = "logs" ]; then
  $COMPOSE logs -f web
  exit 0
fi

# ---- 4. Build + start ----
log "Building + starting TrafficHunt stack..."
if [ "$REBUILD" -eq 1 ]; then
  $COMPOSE build --no-cache web
  $COMPOSE up -d --force-recreate
else
  $COMPOSE up -d --build
fi

# ---- 5. Wait for health ----
log "Waiting for the web container to become healthy (up to ~90s)..."
i=0
while [ "$i" -lt 45 ]; do
  status="$(docker inspect -f '{{if .State.Health}}{{.State.Health.Status}}{{else}}{{.State.Status}}{{end}}' traffichunt 2>/dev/null || echo "missing")"
  case "$status" in
    healthy|running)
      log "Web container is $status."
      break
      ;;
  esac
  i=$((i + 1))
  sleep 2
done

# ---- 6. Status ----
$COMPOSE ps
port="$(grep -E '^PublishPort=' .env | head -n 1 | cut -d= -f2-)"
port="${port:-8080}"
log ""
log "Done. Open the app at:  http://<VPS_IP>:${port}"
log "OAuth note: register http://<VPS_IP>:${port}/oauth/callback in Google Cloud Console"
log "  (Authorized redirect URIs) before connecting the Google account in Settings."
