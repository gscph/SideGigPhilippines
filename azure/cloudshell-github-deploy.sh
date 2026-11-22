# =====================================================================================
# SideGigPh: let GitHub Actions deploy SideGigPh.Api with a MANAGED IDENTITY (no publish profile).
#
# How: open https://shell.azure.com (Bash), paste this WHOLE file, press Enter. About a minute.
# Safe to run again: it skips what exists.
#
# What it does:
#   1. keeps the Function App's own system-assigned identity ON (the API uses it to reach Azure SQL)
#   2. if the app reads its deployment package with that identity, gives it access to the storage
#   3. creates a user-assigned managed identity "sidegigph-github-deploy" for GitHub to sign in as
#   4. trusts GitHub Actions runs of this repo's main branch to act as it (OpenID Connect, no secret)
#   5. lets it deploy to the Function App only (role "Website Contributor" on that one app)
#   6. prints the three values to save as GitHub repo secrets
#
# The two identities are separate: the app's own one talks to SQL, the deploy one only publishes code.
# =====================================================================================

bash -s <<'SIDEGIGPH_GITHUB_DEPLOY'
set -uo pipefail

RESOURCE_GROUP="sidegigph"
FUNCTION_APP="SideGigPhApi"
STORAGE_ACCOUNT="sidegigphbfb9"
DEPLOY_IDENTITY="sidegigph-github-deploy"
GITHUB_REPO="gscph/SideGigPhilippines"   # owner/name
GITHUB_BRANCH="main"                         # the workflow may only deploy from this branch
# GitHub's sign-in token names the repo with its owner and repo ids (owner@id/name@id). If a run fails
# with AADSTS700213, copy the "subject claim" line from that run's log and paste it here.
GITHUB_SUBJECT="repo:gscph@28976144/SideGigPhilippines@1449382288:ref:refs/heads/$GITHUB_BRANCH"

step() { printf '\n== %s\n' "$1"; }
fail() { printf '\n!! STOPPED: %s\n' "$1"; exit 1; }

step "Azure account"
SUBSCRIPTION_ID=$(az account show --query id -o tsv) || fail "not signed in."
TENANT_ID=$(az account show --query tenantId -o tsv)
echo "Subscription $SUBSCRIPTION_ID"

step "Function App $FUNCTION_APP"
APP_GROUP=$(az functionapp list --query "[?name=='$FUNCTION_APP'].resourceGroup | [0]" -o tsv)
[ -n "$APP_GROUP" ] || fail "no Function App named $FUNCTION_APP. Paste cloudshell-setup.sh first."
APP_ID=$(az functionapp show -g "$APP_GROUP" -n "$FUNCTION_APP" --query id -o tsv)
echo "In resource group $APP_GROUP."

step "The app's own identity (for Azure SQL)"
APP_PRINCIPAL=$(az functionapp identity assign -g "$APP_GROUP" -n "$FUNCTION_APP" --query principalId -o tsv) \
  || fail "couldn't turn on the system-assigned identity."
echo "On (principal $APP_PRINCIPAL)."

step "Deployment storage access"
AUTH_TYPE=$(az functionapp show -g "$APP_GROUP" -n "$FUNCTION_APP" \
  --query "functionAppConfig.deployment.storage.authentication.type" -o tsv 2>/dev/null)
echo "The app reads its deployment package with: ${AUTH_TYPE:-unknown}"
if [ "$AUTH_TYPE" = "SystemAssignedIdentity" ]; then
  STORAGE_ID=$(az storage account show -n "$STORAGE_ACCOUNT" --query id -o tsv) \
    || fail "storage account $STORAGE_ACCOUNT not found."
  az role assignment create --assignee-object-id "$APP_PRINCIPAL" --assignee-principal-type ServicePrincipal \
    --role "Storage Blob Data Contributor" --scope "$STORAGE_ID" -o none 2>/dev/null \
    || echo "(already had it, or couldn't add it: check the storage account's Access control (IAM))"
  echo "The app's identity can read and write the deployment container."
fi

step "Deploy identity $DEPLOY_IDENTITY"
az identity create -g "$APP_GROUP" -n "$DEPLOY_IDENTITY" -o none || fail "couldn't create it."
CLIENT_ID=$(az identity show -g "$APP_GROUP" -n "$DEPLOY_IDENTITY" --query clientId -o tsv)
DEPLOY_PRINCIPAL=$(az identity show -g "$APP_GROUP" -n "$DEPLOY_IDENTITY" --query principalId -o tsv)
echo "Ready (client id $CLIENT_ID)."

step "Trust GitHub Actions ($GITHUB_REPO, branch $GITHUB_BRANCH)"
SUBJECT="$GITHUB_SUBJECT"
if az identity federated-credential show -g "$APP_GROUP" --identity-name "$DEPLOY_IDENTITY" -n github-main -o none 2>/dev/null; then
  az identity federated-credential update -g "$APP_GROUP" --identity-name "$DEPLOY_IDENTITY" -n github-main \
    --issuer "https://token.actions.githubusercontent.com" --subject "$SUBJECT" \
    --audiences "api://AzureADTokenExchange" -o none || fail "couldn't update the GitHub trust."
else
  az identity federated-credential create -g "$APP_GROUP" --identity-name "$DEPLOY_IDENTITY" -n github-main \
    --issuer "https://token.actions.githubusercontent.com" --subject "$SUBJECT" \
    --audiences "api://AzureADTokenExchange" -o none || fail "couldn't add the GitHub trust."
fi
echo "Trusted: $SUBJECT"

step "Allow it to deploy to $FUNCTION_APP only"
for attempt in 1 2 3 4 5 6; do
  if az role assignment create --assignee-object-id "$DEPLOY_PRINCIPAL" --assignee-principal-type ServicePrincipal \
       --role "Website Contributor" --scope "$APP_ID" -o none 2>/dev/null; then
    ok=1; break
  fi
  ok=0; echo "Waiting for the new identity to reach Azure AD ($attempt/6)..."; sleep 10
done
[ "$ok" = "1" ] || fail "couldn't give it the Website Contributor role on the app (you need Owner or User Access Administrator)."
echo "Done."

step "Save these as GitHub repo secrets"
echo "GitHub → $GITHUB_REPO → Settings → Secrets and variables → Actions → New repository secret:"
echo
echo "  AZURE_CLIENT_ID        $CLIENT_ID"
echo "  AZURE_TENANT_ID        $TENANT_ID"
echo "  AZURE_SUBSCRIPTION_ID  $SUBSCRIPTION_ID"
echo
echo "None of them is a password. Then: Actions → Deploy API (Azure Functions) → Run workflow (on $GITHUB_BRANCH)."
echo "The old secret AZURE_FUNCTIONAPP_PUBLISH_PROFILE is no longer used; delete it."
SIDEGIGPH_GITHUB_DEPLOY
