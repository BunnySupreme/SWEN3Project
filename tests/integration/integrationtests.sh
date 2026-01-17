#!/usr/bin/env bash
set -eu

(set -o pipefail 2>/dev/null) || true

BASE_URL="${BASE_URL:-http://localhost:5001}"
WORKER_CONTAINER_NAME="${WORKER_CONTAINER_NAME:-paperless-ocrworker}"

SAMPLES_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/samples"

# Endpoint routes
DOC_UPLOAD_PATH="${DOC_UPLOAD_PATH:-/api/documents/upload}"
DOC_GET_PATH_PREFIX="${DOC_GET_PATH_PREFIX:-/api/documents}"

# Auth routes
AUTH_REGISTER_PATH="${AUTH_REGISTER_PATH:-/api/auth/register}"
AUTH_LOGIN_PATH="${AUTH_LOGIN_PATH:-/api/auth/login}"

# Integration test credentials (override in CI)
AUTH_USERNAME="${AUTH_USERNAME:-integration}"
AUTH_PASSWORD="${AUTH_PASSWORD:-ChangeMe_123!}"

TOKEN=""

log()   { printf '[INFO ] %s\n' "$*"; }
fail()  { printf '[ERROR] %s\n' "$*" >&2; exit 1; }

require_cmd() {
  command -v "$1" >/dev/null 2>&1 || fail "Command '$1' not found (please install)"
}

auth_header() {
  [ -n "${TOKEN:-}" ] || fail "TOKEN is empty; login failed"
  printf '%s' "Authorization: Bearer ${TOKEN}"
}

auth_register_if_needed() {
  local url="$BASE_URL$AUTH_REGISTER_PATH"
  local code

  code=$(curl -sS -o /dev/null -w '%{http_code}' \
    -H 'Content-Type: application/json' \
    -d "{\"username\":\"${AUTH_USERNAME}\",\"password\":\"${AUTH_PASSWORD}\"}" \
    "$url" || true)

  if [ "$code" -eq 204 ]; then
    log "Auth register: OK (HTTP 204)"
  elif [ "$code" -eq 400 ]; then
    log "Auth register: 400 (likely already registered) -> continue"
  else
    fail "Auth register: expected 204/400, got: $code"
  fi
}

auth_login() {
  require_cmd jq

  local url="$BASE_URL$AUTH_LOGIN_PATH"
  local response_file
  response_file="$(mktemp)"

  local code
  code=$(curl -sS -o "$response_file" -w '%{http_code}' \
    -H 'Content-Type: application/json' \
    -d "{\"username\":\"${AUTH_USERNAME}\",\"password\":\"${AUTH_PASSWORD}\"}" \
    "$url" || true)

  [ "$code" -eq 200 ] || fail "Auth login: expected 200, got: $code"

  [ -n "$TOKEN" ] || fail "Auth login: could not extract token (check AuthResponse JSON fields)"

  log "Auth login: token acquired"
}

main() {
  require_cmd curl

  if ! command -v jq >/dev/null 2>&1; then
    fail "jq is required now to parse login token; please install jq"
  fi

  log "Running End-To-End tests against $BASE_URL"

  auth_register_if_needed
  auth_login

  test_upload_happy_path
  test_upload_invalid_file
  test_upload_without_file
  test_worker_log_for_upload

  log "All tests passed."
}



# 1) Happy Path: PDF-Upload works
test_upload_happy_path() {
  log "Test: Happy Path – PDF Upload and GET"

  local file="$SAMPLES_DIR/hello.pdf"
  [ -f "$file" ] || fail "Sample-file is missing: $file"

  local url="$BASE_URL$DOC_UPLOAD_PATH"

  local response_file
  response_file="$(mktemp)"

  local http_code
  http_code=$(curl -sS -o "$response_file" -w '%{http_code}' \
    -H "$(auth_header)" \
    -F "file=@${file};type=application/pdf" \
    "$url")

  [ "$http_code" -eq 201 ] || fail "Upload: expected Status 201, received: $http_code"

  log "Upload successful, HTTP $http_code"

  if command -v jq >/dev/null 2>&1; then
    local doc_id
    doc_id=$(jq -r '.id' "$response_file")
    [ "$doc_id" != "null" ] && [ -n "$doc_id" ] || fail "Upload-Response is missing 'id'"

    log "Dokument-ID: $doc_id"

    local get_url="$BASE_URL$DOC_GET_PATH_PREFIX/$doc_id"
    local get_code
    get_code=$(curl -sS -o /tmp/doc.json -w '%{http_code}' \
      -H "$(auth_header)" \
      "$get_url")

    [ "$get_code" -eq 200 ] || fail "GET Document: erwarteter Status 200, erhalten: $get_code"

    log "GET Document successful"
  else
    log "jq missing → skipping response detail check"
  fi
}

# 2) Test: invalid file - wrong Content-Type / not a PDF
test_upload_invalid_file() {
  log "Test: invalid file – wrong Content-Type"

  local file="$SAMPLES_DIR/hello.txt"
  [ -f "$file" ] || { echo "dummy" > "$file"; }

  local url="$BASE_URL$DOC_UPLOAD_PATH"

  local http_code
  http_code=$(curl -sS -o /dev/null -w '%{http_code}' \
    -H "$(auth_header)" \
    -F "file=@${file};type=text/plain" \
    "$url")

  if [ "$http_code" -ne 400 ] && [ "$http_code" -ne 415 ]; then
    fail "Upload invalid file: expected 400/415, got: $http_code"
  fi

  log "Invalid file correctly rejected (HTTP $http_code)"
}

# 3) Test: Upload without file
test_upload_without_file() {
  log "Test: Upload without file"

  local url="$BASE_URL$DOC_UPLOAD_PATH"

  local http_code
  http_code=$(curl -sS -o /dev/null -w '%{http_code}' \
    -H "$(auth_header)" \
    -F "file=" \
    "$url" || true)

  [ "$http_code" -eq 400 ] || fail "Upload without file: expected Status 400, got: $http_code"

  log "Upload without file correctly rejected (HTTP $http_code)"
}

# 4) Worker: processed queue-message?
# checks docker logs
test_worker_log_for_upload() {
  log "Test: Worker processes upload (per docker logs)"

  local file="$SAMPLES_DIR/hello.pdf"
  [ -f "$file" ] || fail "Sample-file missing: $file"

  # remember logs before upload
  local before_log
  before_log="$(docker logs "$WORKER_CONTAINER_NAME" 2>&1 | wc -l || echo 0)"

  # trigger upload again
  local url="$BASE_URL$DOC_UPLOAD_PATH"
  local http_code
  http_code=$(curl -sS -o /dev/null -w '%{http_code}' \
    -H "$(auth_header)" \
    -F "file=@${file};type=application/pdf" \
    "$url")

  [ "$http_code" -eq 201 ] || fail "Upload for Worker-Test: expected 201, got: $http_code"

  # short wait, to ensure worker has time to consume
  sleep 30

  local after_log
  after_log="$(docker logs "$WORKER_CONTAINER_NAME" 2>&1 | wc -l || echo 0)"

  if [ "$after_log" -le "$before_log" ]; then
    fail "Worker-Logs did not change since upload – probably no message processed"
  fi

  log "Worker-Logs increased after Upload → Message probably processed"
}

main "$@"
