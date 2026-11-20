# =====================================================================================
# SideGigPh (Tarbaho): Azure OpenAI embeddings (Azure AI Foundry) for grouping "Others" texts, for Azure Cloud Shell.
#
# How: open https://shell.azure.com (Bash), paste this WHOLE file, press Enter. About 3 minutes.
# Safe to run again: it skips what exists and re-applies the role and the settings.
#
# What it does:
#   1. creates an Azure OpenAI resource (S0, pay per use; there is no free tier) and deploys the model
#      text-embedding-3-small (about $0.02 per 1 million tokens: grouping 10,000 short "Others" texts costs well
#      under $0.01, and each distinct text is embedded once and cached in dbo.OtherJobTextVectors)
#   2. gives the Function App's own managed identity the role "Cognitive Services OpenAI User" on that resource only
#      (it can call the model, not change the resource), so NO key is stored anywhere
#   3. sets the Function App settings EmbeddingsEndpoint and EmbeddingsDeployment
#
# What the API does with it: the operator site's "Others" typed → Groups merges texts that mean the same
# ("linis ng pool" and "pool cleaning"). Only the typed "Others" texts are sent, never names, numbers or uids.
# Without these settings the page still groups identical texts.
#
# Roll back: delete the two settings (the page falls back to identical-text groups).
# =====================================================================================

bash -s <<'SIDEGIGPH_EMBEDDINGS'
set -uo pipefail

RESOURCE_GROUP="sidegigph"
LOCATIONS="southeastasia japaneast eastus"   # tried in this order
OPENAI="sidegigph-openai"
MODEL="text-embedding-3-small"
MODEL_VERSION="1"
DEPLOYMENT="text-embedding-3-small"
DEPLOYMENT_SKU="Standard"                   # Standard keeps the data in the resource's region
FUNCTION_APP="SideGigPhApi"
ROLE="Cognitive Services OpenAI User"

step() { printf '\n== %s\n' "$1"; }
fail() { printf '\n!! STOPPED: %s\n' "$1"; exit 1; }

step "Azure account"
az account show --query "{subscription: name, id: id}" -o tsv \
  || fail "not signed in. Open https://shell.azure.com with the account that pays for Azure."

step "Registering Azure AI services"
az provider register --namespace Microsoft.CognitiveServices --wait >/dev/null \
  || fail "couldn't register Microsoft.CognitiveServices."

step "Azure OpenAI $OPENAI and the $MODEL deployment"
AI_GROUP=$(az cognitiveservices account list --query "[?name=='$OPENAI'].resourceGroup | [0]" -o tsv)
if [ -n "$AI_GROUP" ]; then
  echo "Resource already exists, in resource group $AI_GROUP."
else
  AI_GROUP="$RESOURCE_GROUP"
  az group create --name "$AI_GROUP" --location southeastasia -o none || fail "couldn't create the resource group."
fi

HAS_DEPLOYMENT=$(az cognitiveservices account deployment list -g "$AI_GROUP" -n "$OPENAI" --query "[?name=='$DEPLOYMENT'].name | [0]" -o tsv 2>/dev/null)
if [ -z "$HAS_DEPLOYMENT" ]; then
  DONE=""
  for LOCATION in $LOCATIONS; do
    EXISTS=$(az cognitiveservices account list --query "[?name=='$OPENAI'].location | [0]" -o tsv)
    if [ -z "$EXISTS" ]; then
      az cognitiveservices account create -g "$AI_GROUP" -n "$OPENAI" -l "$LOCATION" --kind OpenAI --sku S0 \
        --custom-domain "$OPENAI" --yes -o none 2>/tmp/ai-error || { echo "No Azure OpenAI in $LOCATION: $(head -c 300 /tmp/ai-error)"; continue; }
      echo "Resource created in $LOCATION."
    fi
    if az cognitiveservices account deployment create -g "$AI_GROUP" -n "$OPENAI" --deployment-name "$DEPLOYMENT" \
         --model-name "$MODEL" --model-version "$MODEL_VERSION" --model-format OpenAI \
         --sku-name "$DEPLOYMENT_SKU" --sku-capacity 10 -o none 2>/tmp/ai-error; then
      DONE="yes"; break
    fi
    echo "The model isn't offered there: $(head -c 300 /tmp/ai-error)"
    # A resource in a region without the model: remove it so the next region can be tried.
    az cognitiveservices account delete -g "$AI_GROUP" -n "$OPENAI" -o none
    az cognitiveservices account purge -g "$AI_GROUP" -n "$OPENAI" -l "$LOCATION" -o none 2>/dev/null
  done
  [ -n "$DONE" ] || fail "couldn't deploy $MODEL in: $LOCATIONS. Add a region to LOCATIONS and paste again."
  echo "Deployed $DEPLOYMENT."
else
  echo "Deployment $DEPLOYMENT already exists."
fi
AI_ID=$(az cognitiveservices account show -g "$AI_GROUP" -n "$OPENAI" --query id -o tsv) || fail "couldn't read it."
ENDPOINT=$(az cognitiveservices account show -g "$AI_GROUP" -n "$OPENAI" --query properties.endpoint -o tsv) || fail "couldn't read the endpoint."

step "The Function App's managed identity may call it (role: $ROLE)"
APP_GROUP=$(az functionapp list --query "[?name=='$FUNCTION_APP'].resourceGroup | [0]" -o tsv)
[ -n "$APP_GROUP" ] || fail "Function App $FUNCTION_APP not found. Paste azure/cloudshell-setup.sh first."
PRINCIPAL=$(az functionapp identity show -g "$APP_GROUP" -n "$FUNCTION_APP" --query principalId -o tsv)
[ -n "$PRINCIPAL" ] || PRINCIPAL=$(az functionapp identity assign -g "$APP_GROUP" -n "$FUNCTION_APP" --query principalId -o tsv) \
  || fail "couldn't turn on the managed identity."
EXISTING=$(az role assignment list --assignee "$PRINCIPAL" --scope "$AI_ID" --role "$ROLE" --query "[0].id" -o tsv)
if [ -z "$EXISTING" ]; then
  az role assignment create --assignee-object-id "$PRINCIPAL" --assignee-principal-type ServicePrincipal \
    --role "$ROLE" --scope "$AI_ID" -o none \
    || fail "couldn't give the role (you need Owner or User Access Administrator on the resource group)."
  echo "Given (can take up to 10 minutes to apply)."
else
  echo "Already has it."
fi

step "Function App settings"
az functionapp config appsettings set -g "$APP_GROUP" -n "$FUNCTION_APP" \
  --settings "EmbeddingsEndpoint=$ENDPOINT" "EmbeddingsDeployment=$DEPLOYMENT" -o none || fail "couldn't save the settings."
echo "EmbeddingsEndpoint and EmbeddingsDeployment are set (no key: the API signs in as its managed identity)."

step "Done"
echo "Endpoint:   $ENDPOINT"
echo "Deployment: $DEPLOYMENT ($MODEL, $DEPLOYMENT_SKU)"
SIDEGIGPH_EMBEDDINGS
