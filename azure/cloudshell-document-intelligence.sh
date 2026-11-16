# =====================================================================================
# SideGigPh (Tarbaho): Azure AI Document Intelligence for the AI-assisted ID check, for Azure Cloud Shell.
#
# How: open https://shell.azure.com (Bash), paste this WHOLE file, press Enter. About 1 minute.
# Safe to run again: it skips what exists and re-applies the settings.
#
# What it does:
#   1. creates the Document Intelligence resource on the FREE tier (F0: 500 pages a month, files up to 4 MB;
#      Azure allows ONE free Document Intelligence resource per subscription)
#   2. creates a PRIVATE container "verification" in the photo storage account for ID photos and proofs
#      (no public access: only the API reads it, unlike "photos")
#   3. puts the endpoint and key into the Function App's settings (DocumentIntelligenceEndpoint,
#      DocumentIntelligenceKey), so they never go into the phone app
#
# The API reads a worker's ID photo with the prebuilt ID model (prebuilt-idDocument: name, ID number, expiry).
# A person still approves every ID; the scan only fills in what the operator checks.
#
# To move to the paid tier later (S0, about $10 per 1,000 pages after the free pages): change SKU below to S0
# and paste again; the endpoint and key stay the same.
# =====================================================================================

bash -s <<'SIDEGIGPH_DOCUMENT_INTELLIGENCE'
set -uo pipefail

RESOURCE_GROUP="sidegigph"
LOCATION="southeastasia"
DOCUMENT_INTELLIGENCE="sidegigph-docintel"
SKU="F0"                          # F0 = free (500 pages/month). S0 = pay as you go.
STORAGE_ACCOUNT="sidegigphbfb9"   # the photo storage account (azure/cloudshell-setup.sh)
FUNCTION_APP="SideGigPhApi"
CONTAINER="verification"

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
az group create --name "$RESOURCE_GROUP" --location "$LOCATION" -o none || fail "couldn't create it."
echo "Ready."

step "Document Intelligence $DOCUMENT_INTELLIGENCE ($SKU)"
# It may live in another resource group, so find it by name.
DI_GROUP=$(az cognitiveservices account list --query "[?name=='$DOCUMENT_INTELLIGENCE'].resourceGroup | [0]" -o tsv)
if [ -n "$DI_GROUP" ]; then
  echo "Already exists, in resource group $DI_GROUP."
  CURRENT_SKU=$(az cognitiveservices account show -g "$DI_GROUP" -n "$DOCUMENT_INTELLIGENCE" --query sku.name -o tsv)
  if [ "$CURRENT_SKU" != "$SKU" ]; then
    az cognitiveservices account update -g "$DI_GROUP" -n "$DOCUMENT_INTELLIGENCE" --sku "$SKU" -o none \
      || fail "couldn't change the tier from $CURRENT_SKU to $SKU."
    echo "Tier changed from $CURRENT_SKU to $SKU."
  fi
else
  DI_GROUP="$RESOURCE_GROUP"
  if [ "$SKU" = "F0" ]; then
    OTHER_FREE=$(az cognitiveservices account list --query "[?kind=='FormRecognizer' && sku.name=='F0'].name | [0]" -o tsv)
    [ -z "$OTHER_FREE" ] \
      || fail "this subscription already has its one free Document Intelligence resource ($OTHER_FREE). Set DOCUMENT_INTELLIGENCE at the top to that name, or SKU to S0, and paste again."
  fi
  az cognitiveservices account create -g "$DI_GROUP" -n "$DOCUMENT_INTELLIGENCE" -l "$LOCATION" \
    --kind FormRecognizer --sku "$SKU" --yes -o none \
    || fail "couldn't create it. If a deleted one with this name is still held, run: az cognitiveservices account purge -g $DI_GROUP -n $DOCUMENT_INTELLIGENCE -l $LOCATION"
  echo "Created."
fi
ENDPOINT=$(az cognitiveservices account show -g "$DI_GROUP" -n "$DOCUMENT_INTELLIGENCE" --query properties.endpoint -o tsv) \
  || fail "couldn't read the endpoint."
KEY=$(az cognitiveservices account keys list -g "$DI_GROUP" -n "$DOCUMENT_INTELLIGENCE" --query key1 -o tsv) \
  || fail "couldn't read the key."
[ -n "$ENDPOINT" ] && [ -n "$KEY" ] || fail "the endpoint or key came back empty."

step "Private container $CONTAINER (ID photos and proofs: no public access)"
STORAGE_GROUP=$(az storage account list --query "[?name=='$STORAGE_ACCOUNT'].resourceGroup | [0]" -o tsv)
[ -n "$STORAGE_GROUP" ] || fail "storage account $STORAGE_ACCOUNT not found. Paste azure/cloudshell-setup.sh first."
CONNECTION=$(az storage account show-connection-string -g "$STORAGE_GROUP" -n "$STORAGE_ACCOUNT" -o tsv) \
  || fail "couldn't read the storage connection string."
az storage container create --name "$CONTAINER" --public-access off --connection-string "$CONNECTION" -o none \
  || fail "couldn't create the container."
az storage container set-permission --name "$CONTAINER" --public-access off --connection-string "$CONNECTION" -o none \
  || fail "couldn't make the container private."
echo "Ready (private)."

step "Function App settings"
APP_GROUP=$(az functionapp list --query "[?name=='$FUNCTION_APP'].resourceGroup | [0]" -o tsv)
[ -n "$APP_GROUP" ] || fail "Function App $FUNCTION_APP not found. Paste azure/cloudshell-setup.sh first."
# Adds or updates only these two settings; the app's other settings are left alone.
az functionapp config appsettings set -g "$APP_GROUP" -n "$FUNCTION_APP" \
  --settings "DocumentIntelligenceEndpoint=$ENDPOINT" "DocumentIntelligenceKey=$KEY" -o none \
  || fail "couldn't save the settings."
echo "DocumentIntelligenceEndpoint and DocumentIntelligenceKey are set."

step "Check: can the key reach the service?"
STATUS=$(curl -s -o /dev/null -w '%{http_code}' \
  -H "Ocp-Apim-Subscription-Key: $KEY" \
  "${ENDPOINT%/}/documentintelligence/documentModels/prebuilt-idDocument?api-version=2024-11-30")
if [ "$STATUS" = "200" ]; then
  echo "Yes: the prebuilt ID model answers."
else
  echo "!! The service answered $STATUS (a new resource can take a few minutes: paste again later to re-check)."
fi

step "Done"
echo "Endpoint: $ENDPOINT"
echo "Tier:     $SKU"
echo "ID photos and proofs go to the private container: $STORAGE_ACCOUNT/$CONTAINER"
echo
echo "For a PC, add to SideGigPh.Api/local.settings.json (never commit the key):"
echo "  \"DocumentIntelligenceEndpoint\": \"$ENDPOINT\","
echo "  \"DocumentIntelligenceKey\": \"<az cognitiveservices account keys list -g $DI_GROUP -n $DOCUMENT_INTELLIGENCE --query key1 -o tsv>\""
SIDEGIGPH_DOCUMENT_INTELLIGENCE
