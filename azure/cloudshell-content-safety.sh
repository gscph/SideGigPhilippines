# =====================================================================================
# SideGigPh (Tarbaho): Azure AI Content Safety for moderating what people write, for Azure Cloud Shell.
#
# How: open https://shell.azure.com (Bash), paste this WHOLE file, press Enter. About 2 minutes.
# Safe to run again: it skips what exists and re-applies the role and the setting.
#
# What it does:
#   1. creates the Content Safety resource on the FREE tier (F0: 5,000 text checks a month; Azure allows one free
#      Content Safety resource per subscription). Tries Southeast Asia, then Japan East if the region doesn't offer it.
#   2. gives the Function App's own managed identity the role "Cognitive Services User" on that resource only
#      (least privilege: it can call the service, not manage it), so NO key is stored anywhere
#   3. sets the Function App setting ContentSafetyEndpoint (the API signs in as its managed identity)
#
# What the API does with it (SideGigPh.Api/Moderation): every chat message, post, offer, review, "Others" text,
# name, listing and suggestion is checked by Tarbaho's own English/Tagalog/jejemon word list first; what passes is
# sent here for sexual, hateful, violent and self-harm content. A refused text is never saved. If this service is
# down or the free checks run out, the word list alone decides (writes never wait on Azure).
#
# Paid tier later (S0, about $0.38 per 1,000 checks after the free ones): set SKU=S0 below and paste again.
# Roll back: az functionapp config appsettings delete -g <group> -n SideGigPhApi --setting-names ContentSafetyEndpoint
#   (the API falls back to the word list only).
# =====================================================================================

bash -s <<'SIDEGIGPH_CONTENT_SAFETY'
set -uo pipefail

RESOURCE_GROUP="sidegigph"
LOCATIONS="southeastasia japaneast"   # tried in this order
CONTENT_SAFETY="sidegigph-contentsafety"
SKU="F0"                              # F0 = free (5,000 checks/month). S0 = pay as you go.
FUNCTION_APP="SideGigPhApi"
ROLE="Cognitive Services User"

step() { printf '\n== %s\n' "$1"; }
fail() { printf '\n!! STOPPED: %s\n' "$1"; exit 1; }

step "Azure account"
az account show --query "{subscription: name, id: id}" -o tsv \
  || fail "not signed in. Open https://shell.azure.com with the account that pays for Azure."

step "Registering Azure AI services (first time takes a minute)"
az provider register --namespace Microsoft.CognitiveServices --wait >/dev/null \
  || fail "couldn't register Microsoft.CognitiveServices."
echo "Done."

step "Resource group $RESOURCE_GROUP"
az group create --name "$RESOURCE_GROUP" --location southeastasia -o none || fail "couldn't create it."
echo "Ready."

step "Content Safety $CONTENT_SAFETY ($SKU)"
CS_GROUP=$(az cognitiveservices account list --query "[?name=='$CONTENT_SAFETY'].resourceGroup | [0]" -o tsv)
if [ -n "$CS_GROUP" ]; then
  echo "Already exists, in resource group $CS_GROUP."
else
  CS_GROUP="$RESOURCE_GROUP"
  CREATED=""
  for LOCATION in $LOCATIONS; do
    if az cognitiveservices account create -g "$CS_GROUP" -n "$CONTENT_SAFETY" -l "$LOCATION" \
         --kind ContentSafety --sku "$SKU" --custom-domain "$CONTENT_SAFETY" --yes -o none 2>/tmp/cs-error; then
      CREATED="$LOCATION"; break
    fi
    echo "Not in $LOCATION: $(head -c 300 /tmp/cs-error)"
  done
  [ -n "$CREATED" ] || fail "couldn't create it in: $LOCATIONS. If the free tier is taken, set SKU=S0; if a deleted one is held, run: az cognitiveservices account purge -g $CS_GROUP -n $CONTENT_SAFETY -l <location>"
  echo "Created in $CREATED."
fi
CS_ID=$(az cognitiveservices account show -g "$CS_GROUP" -n "$CONTENT_SAFETY" --query id -o tsv) || fail "couldn't read it."
ENDPOINT=$(az cognitiveservices account show -g "$CS_GROUP" -n "$CONTENT_SAFETY" --query properties.endpoint -o tsv) \
  || fail "couldn't read the endpoint."

step "The Function App's managed identity may call it (role: $ROLE)"
APP_GROUP=$(az functionapp list --query "[?name=='$FUNCTION_APP'].resourceGroup | [0]" -o tsv)
[ -n "$APP_GROUP" ] || fail "Function App $FUNCTION_APP not found. Paste azure/cloudshell-setup.sh first."
PRINCIPAL=$(az functionapp identity show -g "$APP_GROUP" -n "$FUNCTION_APP" --query principalId -o tsv)
if [ -z "$PRINCIPAL" ]; then
  PRINCIPAL=$(az functionapp identity assign -g "$APP_GROUP" -n "$FUNCTION_APP" --query principalId -o tsv) \
    || fail "couldn't turn on the Function App's managed identity."
fi
EXISTING=$(az role assignment list --assignee "$PRINCIPAL" --scope "$CS_ID" --role "$ROLE" --query "[0].id" -o tsv)
if [ -n "$EXISTING" ]; then
  echo "Already has it."
else
  az role assignment create --assignee-object-id "$PRINCIPAL" --assignee-principal-type ServicePrincipal \
    --role "$ROLE" --scope "$CS_ID" -o none \
    || fail "couldn't give the role (you need Owner or User Access Administrator on the resource group)."
  echo "Given (can take up to 10 minutes to apply)."
fi

step "Function App setting"
az functionapp config appsettings set -g "$APP_GROUP" -n "$FUNCTION_APP" --settings "ContentSafetyEndpoint=$ENDPOINT" -o none \
  || fail "couldn't save the setting."
echo "ContentSafetyEndpoint is set (no key: the API signs in as its managed identity)."

step "Check: does the service answer? (with your own sign-in)"
TOKEN=$(az account get-access-token --resource https://cognitiveservices.azure.com --query accessToken -o tsv)
STATUS=$(curl -s -o /dev/null -w '%{http_code}' -X POST -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"text":"Sira ang gripo sa kusina","outputType":"FourSeverityLevels"}' \
  "${ENDPOINT%/}/contentsafety/text:analyze?api-version=2024-09-01")
case "$STATUS" in
  200) echo "Yes: it answers." ;;
  401|403) echo "It answered $STATUS to YOUR account (you don't need the role; the Function App does). That's fine." ;;
  *) echo "!! It answered $STATUS (a new resource can take a few minutes: paste again later to re-check)." ;;
esac

step "Done"
echo "Endpoint: $ENDPOINT"
echo "Tier:     $SKU"
echo
echo "For a PC, add to SideGigPh.Api/local.settings.json (never commit a key):"
echo "  \"ContentSafetyEndpoint\": \"$ENDPOINT\","
echo "  \"ContentSafetyKey\": \"<az cognitiveservices account keys list -g $CS_GROUP -n $CONTENT_SAFETY --query key1 -o tsv>\""
echo "(on a PC without the key, the API uses your az login if your account has the role)"
SIDEGIGPH_CONTENT_SAFETY
