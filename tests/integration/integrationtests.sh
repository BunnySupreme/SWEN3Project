#!/usr/bin/env bash
set -eu

(set -o pipefail 2>/dev/null) || true

BASE_URL="${BASE_URL:-http://localhost:5001}"
WORKER_CONTAINER_NAME="${WORKER_CONTAINER_NAME:-paperless-ocrworker}"

SAMPLES_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/samples"

# Endpoint routes
DOC_UPLOAD_PATH="${DOC_UPLOAD_PATH:-/api/documents/upload}"
DOC_GET_PATH_PREFIX="${DOC_GET_PATH_PREFIX:-/api/documents}"
DOC_SEARCH_PATH="${DOC_SEARCH_PATH:-/api/documents/search}"

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

  TOKEN="$(jq -r '.accessToken // .token // .jwt // empty' "$response_file")"
  [ -n "$TOKEN" ] || fail "Auth login: could not extract token (check AuthResponse JSON fields). Response: $(cat "$response_file")"

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
  test_login_wrong_password
  test_protected_endpoint_requires_auth
  test_protected_endpoint_rejects_bad_token
  test_logout_revokes_token
  test_search_documents


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

test_login_wrong_password() {
  log "Test: Auth – login fails with wrong password"
  local url="$BASE_URL$AUTH_LOGIN_PATH"
  local code
  code=$(curl -sS -o /tmp/login_fail.json -w '%{http_code}' \
    -H 'Content-Type: application/json' \
    -d "{\"username\":\"${AUTH_USERNAME}\",\"password\":\"${AUTH_PASSWORD}__wrong\"}" \
    "$url" || true)
  [ "$code" -eq 400 ] || fail "Login wrong password: expected 400, got: $code"
}

test_protected_endpoint_requires_auth() {
  log "Test: Auth – protected endpoint rejects missing token"
  local file="$SAMPLES_DIR/hello.pdf"
  [ -f "$file" ] || fail "Sample-file missing: $file"

  local url="$BASE_URL$DOC_UPLOAD_PATH"
  local code
  code=$(curl -sS -o /dev/null -w '%{http_code}' \
    -F "file=@${file};type=application/pdf" \
    "$url" || true)

  # Depending on ASP.NET auth config: 401 or 403.
  if [ "$code" -ne 401 ] && [ "$code" -ne 403 ]; then
    fail "Protected endpoint without token: expected 401/403, got: $code"
  fi
}

test_protected_endpoint_rejects_bad_token() {
  log "Test: Auth – protected endpoint rejects malformed token"
  local file="$SAMPLES_DIR/hello.pdf"
  [ -f "$file" ] || fail "Sample-file missing: $file"

  local url="$BASE_URL$DOC_UPLOAD_PATH"
  local code
  code=$(curl -sS -o /dev/null -w '%{http_code}' \
    -H "Authorization: Bearer not-a-token" \
    -F "file=@${file};type=application/pdf" \
    "$url" || true)

  [ "$code" -eq 401 ] || fail "Protected endpoint bad token: expected 401, got: $code"
}

test_logout_revokes_token() {
  log "Test: Auth – logout revokes token"
  local logout_url="$BASE_URL/api/auth/logout"

  local code
  code=$(curl -sS -o /dev/null -w '%{http_code}' \
    -H "$(auth_header)" \
    -X POST \
    "$logout_url" || true)

  [ "$code" -eq 204 ] || fail "Logout: expected 204, got: $code"

  # After logout, token should no longer work.
  local file="$SAMPLES_DIR/hello.pdf"
  local upload_url="$BASE_URL$DOC_UPLOAD_PATH"
  local upload_code
  upload_code=$(curl -sS -o /dev/null -w '%{http_code}' \
    -H "$(auth_header)" \
    -F "file=@${file};type=application/pdf" \
    "$upload_url" || true)

  [ "$upload_code" -eq 401 ] || fail "Token after logout: expected 401, got: $upload_code"

  # Re-login so later tests still work if needed.
  auth_login
}

test_search_documents() {
  log "Test: Search – requires auth, validates input, returns results"

  local url="$BASE_URL$DOC_SEARCH_PATH"

  # 1) No auth -> 401
  local code
  code=$(curl -sS -o /dev/null -w '%{http_code}' \
    -H 'Content-Type: application/json' \
    -d '{"searchTerm":"hello"}' \
    "$url" || true)
  [ "$code" -eq 401 ] || fail "Search without token: expected 401, got: $code"

  # 2) Empty term -> 400 (authorized)
  code=$(curl -sS -o /tmp/search_empty.json -w '%{http_code}' \
    -H "$(auth_header)" \
    -H 'Content-Type: application/json' \
    -d '{"searchTerm":"   "}' \
    "$url" || true)
  [ "$code" -eq 400 ] || fail "Search empty term: expected 400, got: $code"

  # 3) Non-empty -> 200 and JSON array (authorized)
  code=$(curl -sS -o /tmp/search_ok.json -w '%{http_code}' \
    -H "$(auth_header)" \
    -H 'Content-Type: application/json' \
    -d '{"searchTerm":"hello"}' \
    "$url" || true)
  [ "$code" -eq 200 ] || fail "Search valid term: expected 200, got: $code"

  # Validate it's an array (may be empty)
  require_cmd jq
  jq -e 'type=="array"' /tmp/search_ok.json >/dev/null \
    || fail "Search response is not a JSON array: $(cat /tmp/search_ok.json)"

  log "Search successful (200) and returned JSON array"
}



main "$@"
