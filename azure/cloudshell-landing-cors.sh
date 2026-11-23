# =====================================================================================
# SideGigPh (Tarbaho): let the landing page (tarbaho.com) call the live API, for Azure Cloud Shell.
#
# How: open https://shell.azure.com (Bash), paste this WHOLE file, press Enter. A few seconds.
# Safe to run again: an origin already allowed is left as it is.
#
# The landing page (SideGigPh.FrontDoor, an Azure Static Web App) sends its Early Access form to
# https://api.tarbaho.com/api/early-access from the visitor's browser, a different origin, so the Function App must
# allow the page's addresses. Without this the form says "didn't send".
#
# Never add "*": the allowed origins are exact addresses. To allow another one, add it to ORIGINS below.
# To take one away: az functionapp cors remove -g <group> -n SideGigPhApi --allowed-origins <origin>
# =====================================================================================

bash -s <<'SIDEGIGPH_LANDING_CORS'
set -uo pipefail

FUNCTION_APP="SideGigPhApi"
ORIGINS=("https://www.tarbaho.com" "https://tarbaho.com" "https://delightful-cliff-04e06bd00.8.azurestaticapps.net")

step() { printf '\n== %s\n' "$1"; }
fail() { printf '\n!! STOPPED: %s\n' "$1"; exit 1; }

step "Azure account"
az account show --query "{subscription: name, id: id}" -o tsv \
  || fail "not signed in. Open https://shell.azure.com with the account that pays for Azure."

step "Function App $FUNCTION_APP"
APP_GROUP=$(az functionapp list --query "[?name=='$FUNCTION_APP'].resourceGroup | [0]" -o tsv)
[ -n "$APP_GROUP" ] || fail "no Function App named $FUNCTION_APP in this subscription."
echo "Resource group: $APP_GROUP"

step "Allowed origins (CORS)"
current=$(az functionapp cors show -g "$APP_GROUP" -n "$FUNCTION_APP" --query "allowedOrigins" -o tsv 2>/dev/null || true)
for origin in "${ORIGINS[@]}"; do
  if printf '%s\n' "$current" | grep -qxF "$origin"; then
    echo "already allowed: $origin"
  else
    az functionapp cors add -g "$APP_GROUP" -n "$FUNCTION_APP" --allowed-origins "$origin" -o none \
      || fail "couldn't add $origin."
    echo "added: $origin"
  fi
done

if printf '%s\n' "$current" | grep -qxF "*"; then
  printf '\n!! WARNING: "*" is in the list: any web site can call the API from a browser. Remove it:\n'
  echo "   az functionapp cors remove -g $APP_GROUP -n $FUNCTION_APP --allowed-origins '*'"
fi

step "Done"
az functionapp cors show -g "$APP_GROUP" -n "$FUNCTION_APP" -o table
SIDEGIGPH_LANDING_CORS
