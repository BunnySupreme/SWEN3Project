#!/usr/bin/env bash
set -eu

(set -o pipefail 2>/dev/null) || true

BASE_URL="${BASE_URL:-http://localhost:5001}"
# if we have an HTTP endpoint for OCR worker
WORKER_CONTAINER_NAME="${WORKER_CONTAINER_NAME:-paperless-ocrworker}"

SAMPLES_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/samples"

# Endpoint routes
DOC_UPLOAD_PATH="${DOC_UPLOAD_PATH:-/api/documents/upload}"
DOC_GET_PATH_PREFIX="${DOC_GET_PATH_PREFIX:-/api/documents}"

log()   { printf '[INFO ] %s\n' "$*"; }
fail()  { printf '[ERROR] %s\n' "$*" >&2; exit 1; }

require_cmd() {
  command -v "$1" >/dev/null 2>&1 || fail "Command '$1' not found (please install)"
}

main() {
  require_cmd curl

  if ! command -v jq >/dev/null 2>&1; then
    log "jq not installed, skip detailed JSON responses"
  fi

  log "Running End-To-End tests against $BASE_URL"

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
    -F "file=@${file};type=application/pdf" \
    "$url")

  [ "$http_code" -eq 201 ] || fail "Upload: expected Status 201, received: $http_code"

  log "Upload successful, HTTP $http_code"

  # If jq is available: test ID and metadata
  if command -v jq >/dev/null 2>&1; then
    local doc_id
    doc_id=$(jq -r '.id' "$response_file")
    [ "$doc_id" != "null" ] && [ -n "$doc_id" ] || fail "Upload-Response is missing 'id'"

    log "Dokument-ID: $doc_id"

    local get_url="$BASE_URL$DOC_GET_PATH_PREFIX/$doc_id"
    local get_code
    get_code=$(curl -sS -o /tmp/doc.json -w '%{http_code}' "$get_url")

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
  [ -f "$file" ] || {
    echo "dummy" > "$file"
  }

  local url="$BASE_URL$DOC_UPLOAD_PATH"

  local http_code
  http_code=$(curl -sS -o /dev/null -w '%{http_code}' \
    -F "file=@${file};type=text/plain" \
    "$url")

  # Erwartung: 400 oder 415 – je nach Implementierung
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
    -F "file=@${file};type=application/pdf" \
    "$url")

  [ "$http_code" -eq 201 ] || fail "Upload for Worker-Test: expected 201, got: $http_code"

  # short wait, to insure worker has time to consume
  sleep 2

  local after_log
  after_log="$(docker logs "$WORKER_CONTAINER_NAME" 2>&1 | wc -l || echo 0)"

  if [ "$after_log" -le "$before_log" ]; then
    fail "Worker-Logs did not change since upload – probably no message processed"
  fi

  log "Worker-Logs increased after Upload → Message probably processed"
}

main "$@"
