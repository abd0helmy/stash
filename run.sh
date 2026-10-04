#!/usr/bin/env bash
#
# run.sh — start the whole Stash stack with one command:
#   1. boots infrastructure (postgres, redis, seaweedfs, mailpit) via docker compose
#   2. fills in missing Development user-secrets with dev defaults (never overwrites yours)
#   3. waits until the services are healthy
#   4. creates the S3 bucket if it does not exist
#   5. runs the API (migrations apply automatically in Development)
#
# Usage: ./run.sh
# Stop the infrastructure later with:
#   docker compose -f Docker/docker-compose.yml --env-file Docker/.env down

set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$ROOT_DIR"

COMPOSE_FILE="Docker/docker-compose.yml"
ENV_FILE="Docker/.env"
API_URL="http://localhost:5003"

log()  { printf '[run.sh] %s\n' "$*"; }
fail() { printf '[run.sh] ERROR: %s\n' "$*" >&2; exit 1; }

# --- 0. prerequisites ---------------------------------------------------------
command -v docker >/dev/null 2>&1 || fail "docker is required but not installed."
command -v dotnet >/dev/null 2>&1 || fail ".NET SDK is required but not installed."
command -v openssl >/dev/null 2>&1 || fail "openssl is required but not installed."
docker info >/dev/null 2>&1 || fail "docker daemon is not running. Start Docker Desktop first."
[ -f "$ENV_FILE" ] || fail "$ENV_FILE not found. Recreate it (see README step 2)."

# Values come from Docker/.env (same file docker compose uses).
# shellcheck disable=SC1090
set -a; . "./$ENV_FILE"; set +a
: "${POSTGRES_USER:?}"; : "${POSTGRES_PASSWORD:?}"; : "${POSTGRES_DB:?}"
: "${S3_ACCESS_KEY:?}"; : "${S3_SECRET_KEY:?}"
S3_BUCKET="${S3_BUCKET:-drive}"

compose_id() {
  docker compose -f "$COMPOSE_FILE" --env-file "$ENV_FILE" ps -q "$1"
}

# --- 1. Development secrets (only the missing ones) ----------------------------
ensure_secret() { # ensure_secret <key> <dev-default>
  if dotnet user-secrets list --project Drive.Api 2>/dev/null | grep -q "^$1 ="; then
    log "secret $1 already set — keeping it."
  else
    dotnet user-secrets set "$1" "$2" --project Drive.Api >/dev/null
    log "secret $1 initialized with a dev default."
  fi
}

log "checking Development user-secrets (missing ones get dev defaults)..."
ensure_secret "ConnectionStrings:DefaultConnection" \
  "Host=localhost;Port=5432;Database=${POSTGRES_DB};Username=${POSTGRES_USER};Password=${POSTGRES_PASSWORD}"
ensure_secret "S3:AccessKey" "$S3_ACCESS_KEY"
ensure_secret "S3:SecretKey" "$S3_SECRET_KEY"
ensure_secret "JwtOptions:SecretKey" "$(openssl rand -base64 48)"
ensure_secret "Stripe:SecretKey" "sk_test_dev_only"
ensure_secret "Stripe:PublishableKey" "pk_test_dev_only"
ensure_secret "Stripe:WebhookSecret" "whsec_dev_only"
# Note: Email needs no secrets — it points to the local Mailpit out of the box.

# --- 2. infrastructure ----------------------------------------------------------
log "starting infrastructure (postgres, redis, seaweedfs, mailpit)..."
docker compose -f "$COMPOSE_FILE" --env-file "$ENV_FILE" up -d

wait_until() { # wait_until <seconds> <label> <command...>
  local timeout="$1" label="$2"; shift 2
  local i=0
  while ! "$@" >/dev/null 2>&1; do
    i=$((i + 1))
    if [ "$i" -ge "$timeout" ]; then
      fail "$label did not become ready within ${timeout}s."
    fi
    sleep 1
  done
  log "$label is ready."
}

log "waiting for services to become ready..."
wait_until 60 "postgres" docker exec "$(compose_id postgres)" pg_isready -U "$POSTGRES_USER" -d "$POSTGRES_DB"
wait_until 60 "redis" docker exec "$(compose_id redis)" redis-cli ping
# SeaweedFS answers 403 without a signature — any HTTP response means it is up.
wait_until 120 "seaweedfs S3" curl -s -o /dev/null "http://localhost:8333/"
wait_until 60 "mailpit" curl -s -f -o /dev/null "http://localhost:8025/api/v1/messages"

# --- 3. S3 bucket (idempotent) ---------------------------------------------------
# Throwaway amazon/aws-cli container sharing seaweedfs' network, so no local
# AWS CLI install is needed and it works on Docker Desktop too.
aws_s3() {
  docker run --rm \
    --network "container:$(compose_id seaweedfs)" \
    -e "AWS_ACCESS_KEY_ID=${S3_ACCESS_KEY}" \
    -e "AWS_SECRET_ACCESS_KEY=${S3_SECRET_KEY}" \
    -e AWS_DEFAULT_REGION=us-east-1 \
    -e AWS_EC2_METADATA_DISABLED=true \
    amazon/aws-cli --endpoint-url http://localhost:8333 s3 "$@"
}

log "ensuring S3 bucket 's3://${S3_BUCKET}' exists..."
if aws_s3 ls "s3://${S3_BUCKET}" >/dev/null 2>&1; then
  log "bucket 's3://${S3_BUCKET}' already exists."
else
  aws_s3 mb "s3://${S3_BUCKET}" || fail "could not create S3 bucket 's3://${S3_BUCKET}'."
fi

# --- 4. API ----------------------------------------------------------------------
if curl -s -o /dev/null --max-time 2 "${API_URL}/" 2>/dev/null; then
  fail "something is already listening on ${API_URL} (a previous API process?). Stop it first: lsof -ti :5003 | xargs kill -9"
fi
log "starting API at ${API_URL} (Ctrl+C to stop; infra keeps running)..."
log "API docs: ${API_URL}/scalar | Mailpit UI: http://localhost:8025"
trap 'log "API stopped. Infra still running — stop it with: docker compose -f Docker/docker-compose.yml --env-file Docker/.env down"' EXIT
dotnet run --project Drive.Api --profile http
