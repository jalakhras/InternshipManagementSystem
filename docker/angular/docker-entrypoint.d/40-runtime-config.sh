#!/bin/sh
# Writes the SPA's runtime configuration from environment variables.
#
# The nginx images run every executable in /docker-entrypoint.d before starting
# the server, so this happens once per container start, before the first request.
#
# Why at all: a built Angular bundle is static files with the API URL compiled in.
# Rebuilding the image per environment means the artifact that was tested is not
# the artifact that ships. Instead the bundle carries defaults and reads this one
# small file, which is the only thing that differs between environments.
set -eu

TARGET="/usr/share/nginx/html/assets/config.json"

# Absent variables are left out of the file entirely rather than written as empty
# strings, because the app treats an absent key as "keep the compiled default" and
# an empty one as a deliberate blank. SPA_BASE_URL is normally absent on purpose:
# the app then reads its own origin from the page, which is right more often than
# a value typed into a deployment file — and is the only thing that works when the
# same image is reached over several hostnames.
emit() {
  name="$1"
  value="$2"
  [ -n "$value" ] || return 0
  printf '  "%s": %s,\n' "$name" "$value" >> "$TARGET.tmp"
}

# Minimal JSON string escaping. These values are URLs and identifiers, but a stray
# quote or backslash would produce a file the app cannot parse, and it would fail
# at boot with nothing but a console error to explain it.
json_string() {
  [ -n "${1:-}" ] || return 0
  printf '"%s"' "$(printf '%s' "$1" | sed -e 's/\\/\\\\/g' -e 's/"/\\"/g')"
}

json_bool() {
  case "$(printf '%s' "${1:-}" | tr '[:upper:]' '[:lower:]')" in
    true|1|yes)  printf 'true' ;;
    false|0|no)  printf 'false' ;;
    *)           return 0 ;;
  esac
}

mkdir -p "$(dirname "$TARGET")"
: > "$TARGET.tmp"

emit apiUrl       "$(json_string "${API_URL:-}")"
emit issuer       "$(json_string "${OAUTH_ISSUER:-${API_URL:-}}")"
emit baseUrl      "$(json_string "${SPA_BASE_URL:-}")"
emit clientId     "$(json_string "${OAUTH_CLIENT_ID:-}")"
emit scope        "$(json_string "${OAUTH_SCOPE:-}")"
emit appName      "$(json_string "${APP_NAME:-}")"
emit requireHttps "$(json_bool   "${OAUTH_REQUIRE_HTTPS:-}")"

if [ ! -s "$TARGET.tmp" ]; then
  # Nothing was supplied. Leave the file the image was built with rather than
  # writing an empty object: a developer running the image bare still gets the
  # local defaults, and the difference between "unconfigured" and "configured to
  # nothing" stays visible.
  rm -f "$TARGET.tmp"
  echo "40-runtime-config.sh: no configuration in the environment; keeping $TARGET as built"
  exit 0
fi

# Wrap the accumulated lines into an object, dropping the trailing comma from the
# last one so the result is valid JSON rather than something the browser rejects.
{
  echo '{'
  sed '$ s/,$//' "$TARGET.tmp"
  echo '}'
} > "$TARGET"

rm -f "$TARGET.tmp"

echo "40-runtime-config.sh: wrote $TARGET"
cat "$TARGET"
