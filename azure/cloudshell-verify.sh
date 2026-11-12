# =====================================================================================
# SideGigPh: check the Azure photo setup. READ-ONLY: changes nothing.
#
# How: in https://shell.azure.com (Bash), paste this WHOLE file, press Enter. Run it after
# cloudshell-setup.sh and after publishing SideGigPh.Api from Visual Studio.
# Each line says PASS or FAIL (with what to do). A summary is at the end.
# The names must match the ones at the top of cloudshell-setup.sh.
# =====================================================================================

bash -s <<'SIDEGIGPH_AZURE_VERIFY'
set -uo pipefail

STORAGE_ACCOUNT="sidegigphbfb9"
FUNCTION_APP="SideGigPhApi"
CONTAINER="photos"

passed=0; failed=0
pass() { printf '  PASS  %s\n' "$1"; passed=$((passed + 1)); }
fail() { printf '  FAIL  %s\n' "$1"; failed=$((failed + 1)); }
step() { printf '\n== %s\n' "$1"; }
# The error code Azure Storage puts in a response header (empty when there is none).
error_code() { curl -s -o /dev/null -D - "$1" | tr -d '\r' | awk -F': ' 'tolower($1)=="x-ms-error-code"{print $2}'; }
status_of() { curl -s -o /dev/null -w '%{http_code}' "$@"; }

step "Storage"
STORAGE_GROUP=$(az storage account list --query "[?name=='$STORAGE_ACCOUNT'].resourceGroup | [0]" -o tsv)
if [ -n "$STORAGE_GROUP" ]; then
  pass "storage account $STORAGE_ACCOUNT exists (resource group $STORAGE_GROUP)"
  [ "$(az storage account show -g "$STORAGE_GROUP" -n "$STORAGE_ACCOUNT" --query allowBlobPublicAccess -o tsv)" = "true" ] \
    && pass "photo URLs may be public" || fail "public photo URLs are blocked on the account: run cloudshell-setup.sh"
  base="https://$STORAGE_ACCOUNT.blob.core.windows.net/$CONTAINER"
  # A made-up photo: a public container answers "BlobNotFound"; a private or missing one doesn't.
  code=$(error_code "$base/0000000000000000000000000000000000000000000000000000000000000000.jpg")
  [ "$code" = "BlobNotFound" ] && pass "container $CONTAINER exists and photos open by URL" \
    || fail "photos don't open by URL (Azure says ${code:-nothing}): run cloudshell-setup.sh"
  code=$(error_code "$base?restype=container&comp=list")
  [ -n "$code" ] && pass "nobody can list all photos" \
    || fail "anyone can list every photo: set the container's access level to Blob, not Container"
else
  fail "no storage account $STORAGE_ACCOUNT: run cloudshell-setup.sh"
fi

step "Function App"
APP_GROUP=$(az functionapp list --query "[?name=='$FUNCTION_APP'].resourceGroup | [0]" -o tsv)
if [ -n "$APP_GROUP" ]; then
  pass "$FUNCTION_APP exists"
  names=$(az functionapp config appsettings list -g "$APP_GROUP" -n "$FUNCTION_APP" --query "[].name" -o tsv)
  for setting in PhotoStorage FirebaseProjectId; do
    grep -qx "$setting" <<<"$names" && pass "setting $setting is there" \
      || fail "setting $setting is missing: run cloudshell-setup.sh"
  done
  host=$(az functionapp show -g "$APP_GROUP" -n "$FUNCTION_APP" --query defaultHostName -o tsv)
  # Without a sign-in token the API must refuse (401). 404 means the code isn't published yet.
  status=$(status_of -X POST "https://$host/api/photos" -H "Content-Type: image/jpeg" --data-binary "x")
  case "$status" in
    401) pass "the photo API is published and refuses requests without a sign-in" ;;
    404) fail "the photo API isn't published yet: Visual Studio > SideGigPh.Api > Publish" ;;
    500|503) fail "the photo API is crashing ($status): Azure portal > $FUNCTION_APP > Log stream" ;;
    *) fail "the photo API answered $status, expected 401" ;;
  esac
  echo "  API address for the app: https://$host/api/"
else
  fail "no Function App $FUNCTION_APP: run cloudshell-setup.sh"
fi

printf '\n== Summary: %d passed, %d failed\n' "$passed" "$failed"
[ "$failed" -eq 0 ] && echo "Everything is set up." || echo "Fix the FAIL lines, then paste this again."
SIDEGIGPH_AZURE_VERIFY
