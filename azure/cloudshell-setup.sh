# =====================================================================================
# SideGigPh: Azure photo storage + the SideGigPh.Api Function App, for Azure Cloud Shell.
#
# How: open https://shell.azure.com (Bash), paste this WHOLE file, press Enter. 2–5 minutes.
# Safe to run again: it skips what exists and re-applies the settings.
#
# What it does:
#   1. creates a resource group in Southeast Asia (Singapore, closest to the Philippines)
#   2. creates a storage account and a "photos" container that anyone can read by exact URL
#      (photo ids are unguessable SHA-256 hashes; nobody can list the container)
#   3. creates the Function App that SideGigPh.Api is published to (Flex Consumption plan)
#   4. puts the storage CONNECTION STRING into the Function App's settings (PhotoStorage),
#      so you never copy it by hand and it never goes into the phone app
#
# After it: publish SideGigPh.Api from Visual Studio (right-click > Publish > Azure > this app),
# then paste azure/cloudshell-verify.sh.
#
# Names marked "globally unique" must be free across all of Azure. If one is taken, change it
# here and paste again.
# =====================================================================================

bash -s <<'SIDEGIGPH_AZURE_SETUP'
set -uo pipefail

RESOURCE_GROUP="sidegigph"
LOCATION="southeastasia"
STORAGE_ACCOUNT="sidegigphbfb9"   # photos go in its "photos" container; also the Function App's storage
                                  # https://sidegigphbfb9.blob.core.windows.net/photos/
FUNCTION_APP="SideGigPhApi"   # https://sidegigphapi-hrb9bgerbgcgawbm.southeastasia-01.azurewebsites.net
FIREBASE_PROJECT_ID="sidegigphilippines"
DOTNET_VERSION="10.0"
CONTAINER="photos"

step() { printf '\n== %s\n' "$1"; }
fail() { printf '\n!! STOPPED: %s\n' "$1"; exit 1; }

step "Azure account"
az account show --query "{subscription: name, id: id}" -o tsv \
  || fail "not signed in. Open https://shell.azure.com with the account that pays for Azure."

step "Registering the Azure services this needs (first time takes a minute)"
for provider in Microsoft.Storage Microsoft.Web Microsoft.App Microsoft.Insights Microsoft.OperationalInsights; do
  az provider register --namespace "$provider" --wait >/dev/null || fail "couldn't register $provider."
done
echo "Done."

step "Resource group $RESOURCE_GROUP"
az group create --name "$RESOURCE_GROUP" --location "$LOCATION" -o none || fail "couldn't create it."
echo "Ready."

step "Storage account $STORAGE_ACCOUNT"
# It may live in another resource group (it was created in the portal), so find it by name.
STORAGE_GROUP=$(az storage account list --query "[?name=='$STORAGE_ACCOUNT'].resourceGroup | [0]" -o tsv)
if [ -n "$STORAGE_GROUP" ]; then
  echo "Already exists, in resource group $STORAGE_GROUP."
  # Lets a container opt in to public read. Existing containers stay private: only "photos" opts in below.
  az storage account update -g "$STORAGE_GROUP" -n "$STORAGE_ACCOUNT" --allow-blob-public-access true -o none \
    || fail "couldn't allow public photo URLs on it."
else
  STORAGE_GROUP="$RESOURCE_GROUP"
  [ "$(az storage account check-name -n "$STORAGE_ACCOUNT" --query nameAvailable -o tsv)" = "true" ] \
    || fail "the name $STORAGE_ACCOUNT is taken. Change STORAGE_ACCOUNT at the top and paste again."
  az storage account create -g "$STORAGE_GROUP" -n "$STORAGE_ACCOUNT" -l "$LOCATION" \
    --sku Standard_LRS --kind StorageV2 --min-tls-version TLS1_2 \
    --allow-blob-public-access true --allow-shared-key-access true -o none \
    || fail "couldn't create the storage account."
  echo "Created."
fi
CONNECTION=$(az storage account show-connection-string -g "$STORAGE_GROUP" -n "$STORAGE_ACCOUNT" -o tsv) \
  || fail "couldn't read the connection string."
STORAGE_ID=$(az storage account show -g "$STORAGE_GROUP" -n "$STORAGE_ACCOUNT" --query id -o tsv)

step "Container $CONTAINER (photos readable by exact URL, not listable)"
az storage container create --name "$CONTAINER" --public-access blob --connection-string "$CONNECTION" -o none \
  || fail "couldn't create the container."
az storage container set-permission --name "$CONTAINER" --public-access blob --connection-string "$CONNECTION" -o none \
  || fail "couldn't make photos readable."
echo "Ready."

step "Function App $FUNCTION_APP"
# It may already exist in another resource group, so find it by name.
APP_GROUP=$(az functionapp list --query "[?name=='$FUNCTION_APP'].resourceGroup | [0]" -o tsv)
if [ -n "$APP_GROUP" ]; then
  echo "Already exists, in resource group $APP_GROUP."
  echo "Runtime: $(az functionapp show -g "$APP_GROUP" -n "$FUNCTION_APP" \
    --query "join(' ', [kind, to_string(siteConfig.linuxFxVersion), to_string(functionAppConfig.runtime)])" -o tsv)"
  echo "SideGigPh.Api needs .NET $DOTNET_VERSION isolated on Linux; if the line above says otherwise, publishing will fail."
  # Publishing replaces EVERY function in the app with SideGigPh.Api's. Show what's there now.
  others=$(az functionapp function list -g "$APP_GROUP" -n "$FUNCTION_APP" --query "[].name" -o tsv 2>/dev/null \
    | sed 's|.*/||' | grep -vx -e UploadPhoto -e DeletePhoto)
  if [ -n "$others" ]; then
    echo
    echo "!! WARNING: $FUNCTION_APP already runs other functions:"
    sed 's/^/     /' <<<"$others"
    echo "!! Publishing SideGigPh.Api to it DELETES them. Use a separate Function App instead"
    echo "!! (set FUNCTION_APP at the top to a new name and paste again), unless you mean to replace them."
  fi
else
  APP_GROUP="$RESOURCE_GROUP"
  az functionapp create -g "$APP_GROUP" -n "$FUNCTION_APP" --storage-account "$STORAGE_ID" \
    --flex-consumption-location "$LOCATION" --runtime dotnet-isolated --runtime-version "$DOTNET_VERSION" -o none \
    || fail "couldn't create it. If the name is taken, change FUNCTION_APP at the top. If .NET $DOTNET_VERSION isn't offered, the error lists the versions that are."
  echo "Created."
fi

step "Function App settings (this is where the connection string goes)"
# Adds or updates only these two settings; the app's other settings are left alone.
az functionapp config appsettings set -g "$APP_GROUP" -n "$FUNCTION_APP" \
  --settings "PhotoStorage=$CONNECTION" "FirebaseProjectId=$FIREBASE_PROJECT_ID" -o none \
  || fail "couldn't save the settings."
echo "PhotoStorage and FirebaseProjectId are set."

HOST=$(az functionapp show -g "$APP_GROUP" -n "$FUNCTION_APP" --query defaultHostName -o tsv)
step "Done"
echo "API address for the app:  https://$HOST/api/"
echo "Photos are served from:   https://$STORAGE_ACCOUNT.blob.core.windows.net/$CONTAINER/<id>.jpg"
echo
echo "Next: Visual Studio > right-click SideGigPh.Api > Publish > Azure > Azure Function App > $FUNCTION_APP."
echo "Then paste azure/cloudshell-verify.sh here."
SIDEGIGPH_AZURE_SETUP
