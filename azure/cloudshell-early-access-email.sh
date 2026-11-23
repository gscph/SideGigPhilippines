# =====================================================================================
# SideGigPh (Tarbaho): email for the landing page's Early Access sign-ups, for Azure Cloud Shell.
#
# How: open https://shell.azure.com (Bash), paste this WHOLE file, press Enter. It runs in two passes:
#   1st paste: creates the email service and the domain tarbaho.com, then prints the DNS records to add at
#              Cloudflare (each one "DNS only", grey cloud) and stops.
#   2nd paste (15+ minutes after adding them): checks the records, connects the domain, and puts the settings
#              into the Function App. Paste again as often as you like: it skips what is already done.
#
# What it does:
#   1. creates an Email Communication Service with the custom domain tarbaho.com (data kept in Asia Pacific,
#      as the privacy policy says) and asks Azure to verify the domain, SPF and both DKIM records
#   2. creates a Communication Services resource and connects the verified domain to it
#   3. puts three settings into the Function App, so they never go into the repo or the site:
#        EmailConnection  = the Communication Services connection string
#        EarlyAccessFrom  = DoNotReply@tarbaho.com
#        EarlyAccessTo    = the mailbox that gets each sign-up (asked for here; never written in this file:
#                           the repo is public). Already set? Press Enter to keep it.
#   4. lets the landing page's addresses call the API from a browser (CORS; never "*")
#
# Skip the question: EARLY_ACCESS_TO=you@example.com before pasting (export EARLY_ACCESS_TO=...).
# =====================================================================================

bash -s <<'SIDEGIGPH_EARLY_ACCESS_EMAIL'
set -uo pipefail

RESOURCE_GROUP="sidegigph"
FUNCTION_APP="SideGigPhApi"
EMAIL_SERVICE="sidegigph-email"
COMMUNICATION="sidegigph-communication"   # globally unique: if taken, change it here and paste again
DATA_LOCATION="Asia Pacific"
DOMAIN="tarbaho.com"
SENDER="DoNotReply@tarbaho.com"           # exists by default on a connected custom domain
ORIGINS=("https://tarbaho.com" "https://www.tarbaho.com" "https://delightful-cliff-04e06bd00.8.azurestaticapps.net")

step() { printf '\n== %s\n' "$1"; }
fail() { printf '\n!! STOPPED: %s\n' "$1"; exit 1; }

az config set extension.use_dynamic_install=yes_without_prompt --only-show-errors >/dev/null 2>&1

step "Azure account"
az account show --query "{subscription: name, id: id}" -o tsv \
  || fail "not signed in. Open https://shell.azure.com with the account that pays for Azure."

step "Registering Azure Communication Services (first time takes a minute)"
az provider register --namespace Microsoft.Communication --wait >/dev/null \
  || fail "couldn't register Microsoft.Communication."
echo "Done."

step "Function App $FUNCTION_APP"
APP_GROUP=$(az functionapp list --query "[?name=='$FUNCTION_APP'].resourceGroup | [0]" -o tsv)
[ -n "$APP_GROUP" ] || fail "Function App $FUNCTION_APP not found. Paste azure/cloudshell-setup.sh first."
echo "Resource group: $APP_GROUP"
az group show -n "$RESOURCE_GROUP" -o none 2>/dev/null \
  || fail "resource group $RESOURCE_GROUP not found. Paste azure/cloudshell-setup.sh first."

step "Email Communication Service $EMAIL_SERVICE"
EMAIL_GROUP=$(az resource list --resource-type Microsoft.Communication/emailServices \
  --query "[?name=='$EMAIL_SERVICE'].resourceGroup | [0]" -o tsv)
if [ -n "$EMAIL_GROUP" ]; then
  echo "Already exists, in resource group $EMAIL_GROUP."
else
  EMAIL_GROUP="$RESOURCE_GROUP"
  az communication email create -n "$EMAIL_SERVICE" -g "$EMAIL_GROUP" --location global \
    --data-location "$DATA_LOCATION" --only-show-errors -o none \
    || fail "couldn't create it."
  echo "Created (data location: $DATA_LOCATION)."
fi

step "Domain $DOMAIN"
JUST_ADDED=no
if az communication email domain show --domain-name "$DOMAIN" --email-service-name "$EMAIL_SERVICE" \
     -g "$EMAIL_GROUP" --only-show-errors -o none 2>/dev/null; then
  echo "Already added."
else
  az communication email domain create --domain-name "$DOMAIN" --email-service-name "$EMAIL_SERVICE" \
    -g "$EMAIL_GROUP" --location global --domain-management CustomerManaged --only-show-errors -o none \
    || fail "couldn't add the domain."
  JUST_ADDED=yes
  echo "Added (you manage its DNS, at Cloudflare)."
fi

domain_field() {
  az communication email domain show --domain-name "$DOMAIN" --email-service-name "$EMAIL_SERVICE" \
    -g "$EMAIL_GROUP" --only-show-errors --query "$1" -o tsv
}
DOMAIN_ID=$(domain_field id)
[ -n "$DOMAIN_ID" ] || fail "couldn't read the domain."

step "DNS records for Cloudflare (DNS → Records → Add record; Proxy status: DNS only)"
echo "Name '@' means $DOMAIN itself. Copy each value exactly."
for kind in Domain SPF DKIM DKIM2 DMARC; do   # Azure's own (capitalised) key names
  records="(verificationRecords || properties.verificationRecords).$kind"
  record=$(domain_field "$records && join('|', $records.[type, name, value])")
  [ -n "$record" ] || continue
  IFS='|' read -r rtype rname rvalue <<<"$record"
  [ "$rname" = "$DOMAIN" ] && rname="@"
  rname="${rname%.$DOMAIN}"
  printf '\n  %-7s %s\n  Name:   %s\n  Value:  %s\n' "$rtype" "$kind" "$rname" "$rvalue"
done
echo
echo "SPF: a domain has ONE SPF record. If Cloudflare already shows a TXT starting with v=spf1 at @, don't add a"
echo "second one: put 'include:spf.protection.outlook.com' into the one you have, before its last word (-all / ~all)."
echo "DMARC is optional but helps the mail reach the inbox; it is not checked here."

step "Verification"
domain_status() { domain_field "(verificationStates || properties.verificationStates).$1.status"; }
ALL_VERIFIED=yes
for kind in Domain SPF DKIM DKIM2; do
  status=$(domain_status "$kind")
  case "$status" in Verified|VerificationRequested|VerificationInProgress) ask=no ;; *) ask=yes ;; esac
  # The run that just added the domain only prints the records: Cloudflare can't have them yet.
  if [ "$ask" = yes ] && [ "$JUST_ADDED" = no ]; then
    # Ask Azure to look up the record now (it waits for the answer), then read the result.
    az communication email domain initiate-verification --domain-name "$DOMAIN" \
      --email-service-name "$EMAIL_SERVICE" -g "$EMAIL_GROUP" --verification-type "$kind" \
      --only-show-errors -o none 2>/dev/null
    status=$(domain_status "$kind")
  fi
  case "$status" in
    Verified)
      echo "$kind: verified" ;;
    VerificationRequested|VerificationInProgress)
      ALL_VERIFIED=no; echo "$kind: Azure is still checking" ;;
    *)
      ALL_VERIFIED=no
      if [ "$JUST_ADDED" = yes ]; then
        echo "$kind: not checked yet (add its record first)"
      else
        echo "$kind: not found (${status:-not started}): check its record above at Cloudflare"
      fi ;;
  esac
done
if [ "$ALL_VERIFIED" != yes ]; then
  step "Paused: waiting for DNS"
  echo "Add the records above at Cloudflare (DNS only), wait 15 minutes or more, then paste this file again."
  echo "Nothing else was changed: the Function App doesn't have the email settings yet, so the form answers \"didn't send\"."
  exit 0
fi

step "Communication Services $COMMUNICATION"
COMM_GROUP=$(az resource list --resource-type Microsoft.Communication/communicationServices \
  --query "[?name=='$COMMUNICATION'].resourceGroup | [0]" -o tsv)
if [ -n "$COMM_GROUP" ]; then
  echo "Already exists, in resource group $COMM_GROUP."
else
  COMM_GROUP="$RESOURCE_GROUP"
  az communication create -n "$COMMUNICATION" -g "$COMM_GROUP" --location global \
    --data-location "$DATA_LOCATION" --only-show-errors -o none \
    || fail "couldn't create it. If the name is taken, change COMMUNICATION at the top and paste again."
  echo "Created (data location: $DATA_LOCATION)."
fi

step "Connecting $DOMAIN to $COMMUNICATION"
LINKED=$(az communication show -n "$COMMUNICATION" -g "$COMM_GROUP" --only-show-errors \
  --query "(linkedDomains || properties.linkedDomains)[]" -o tsv | tr A-Z a-z)
if printf '%s\n' "$LINKED" | grep -qxF "$(echo "$DOMAIN_ID" | tr A-Z a-z)"; then
  echo "Already connected."
else
  # --linked-domains replaces the list, so keep any domain already connected.
  az communication update -n "$COMMUNICATION" -g "$COMM_GROUP" --only-show-errors -o none \
    --linked-domains $(az communication show -n "$COMMUNICATION" -g "$COMM_GROUP" --only-show-errors \
       --query "(linkedDomains || properties.linkedDomains)[]" -o tsv) "$DOMAIN_ID" \
    || fail "couldn't connect the domain."
  echo "Connected. Sender: $SENDER"
fi

CONNECTION=$(az communication list-key -n "$COMMUNICATION" -g "$COMM_GROUP" --only-show-errors \
  --query primaryConnectionString -o tsv) || fail "couldn't read the connection string."
[ -n "$CONNECTION" ] || fail "the connection string came back empty."

step "Who gets the sign-ups (EarlyAccessTo)"
HAS_TO=$(az functionapp config appsettings list -g "$APP_GROUP" -n "$FUNCTION_APP" \
  --query "[?name=='EarlyAccessTo'] | length(@)" -o tsv)
TO="${EARLY_ACCESS_TO:-}"
if [ -z "$TO" ]; then
  if [ "$HAS_TO" = "1" ]; then
    printf 'Already set. New address, or Enter to keep it: '
  else
    printf 'Email address that should get each sign-up: '
  fi
  read -r TO </dev/tty || TO=""
fi
TO=$(echo "$TO" | tr -d '[:space:]')
if [ -n "$TO" ]; then
  [[ "$TO" =~ ^[^@]+@[^@]+\.[^@]+$ ]] || fail "\"$TO\" doesn't look like an email address. Paste again."
elif [ "$HAS_TO" != "1" ]; then
  fail "no address given. Paste again and type the mailbox that should get the sign-ups."
fi

step "Function App settings"
# Adds or updates only these settings; the app's other settings are left alone.
SETTINGS=("EmailConnection=$CONNECTION" "EarlyAccessFrom=$SENDER")
[ -n "$TO" ] && SETTINGS+=("EarlyAccessTo=$TO")
az functionapp config appsettings set -g "$APP_GROUP" -n "$FUNCTION_APP" --settings "${SETTINGS[@]}" -o none \
  || fail "couldn't save the settings."
echo "EmailConnection and EarlyAccessFrom are set; EarlyAccessTo $([ -n "$TO" ] && echo "is set" || echo "kept as it was")."

step "Allowed origins (CORS): the landing page may call the API"
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
echo "Sign-ups are emailed from $SENDER. The Function App restarts with the new settings by itself."
echo "If the live API is older than the Early Access form, run Deploy API in GitHub Actions first."
echo "Then open https://tarbaho.com, sign up with your own address, and look in the inbox (and Spam the first time)."
SIDEGIGPH_EARLY_ACCESS_EMAIL
