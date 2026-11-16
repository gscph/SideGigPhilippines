# =====================================================================================
# SideGigPh: cost guardrails for Azure, for Azure Cloud Shell (docs/server-cost-plan.md, item 0).
#
# How: open https://shell.azure.com (Bash) with the account that pays for Azure, paste this WHOLE
# file, press Enter. About a minute. Safe to run again: it re-applies the same settings.
#
# What it does (each step says what it changed, or why it skipped):
#   1. Azure SQL database: serverless, at most 1 vCore, at least 0.5, pauses after 15 min idle
#      (the shortest Azure allows). The bill is per second while it is awake, so pausing early is
#      the biggest saving. A database on the free offer is left alone (it is already capped).
#   2. Function App (Flex Consumption): 512 MB instances (the free grant lasts 4x longer than at
#      2 GB), at most 2 instances, so a bug or an attack can't scale the bill up.
#   3. Application Insights / Log Analytics: a daily cap of 0.1 GB of logs.
#   4. Prints the two things only the portal can do: budget alerts (Azure and Firebase), and
#      Firebase App Check (see the end of the output).
#
# Undo: run it again with other numbers below.
# =====================================================================================

bash -s <<'SIDEGIGPH_COST'
set -uo pipefail

SQL_SERVER="sidegigph"          # sidegigph.database.windows.net
DATABASE="SideGigPh"
FUNCTION_APP="SideGigPhApi"
SQL_MAX_VCORES=1                # GP_S_Gen5_1: plenty for an early marketplace
SQL_MIN_VCORES=0.5
SQL_PAUSE_AFTER_MINUTES=15      # the shortest auto-pause delay Azure allows
FUNCTION_MEMORY_MB=512
FUNCTION_MAX_INSTANCES=2
LOG_CAP_GB=0.1

step() { printf '\n== %s\n' "$1"; }
note() { printf '   %s\n' "$1"; }

step "Azure account"
az account show --query "{subscription: name, id: id}" -o tsv \
  || { echo "!! Not signed in. Open https://shell.azure.com with the account that pays for Azure."; exit 1; }

step "Azure SQL: $SQL_SERVER / $DATABASE"
SQL_GROUP=$(az sql server list --query "[?name=='$SQL_SERVER'].resourceGroup | [0]" -o tsv)
if [ -z "$SQL_GROUP" ]; then
  note "SQL server $SQL_SERVER not found in this subscription: skipped."
else
  read -r SKU FREE PAUSE <<<"$(az sql db show -g "$SQL_GROUP" -s "$SQL_SERVER" -n "$DATABASE" \
    --query "[currentServiceObjectiveName, to_string(useFreeLimit), to_string(autoPauseDelay)]" -o tsv | tr '\n' ' ')"
  note "Now: $SKU, free offer: $FREE, pauses after: $PAUSE min"
  if [ "$FREE" = "true" ]; then
    note "On the free offer (100,000 vCore-seconds a month, then it pauses): left as it is."
  else
    az sql db update -g "$SQL_GROUP" -s "$SQL_SERVER" -n "$DATABASE" \
      --edition GeneralPurpose --compute-model Serverless --family Gen5 \
      --capacity "$SQL_MAX_VCORES" --min-capacity "$SQL_MIN_VCORES" \
      --auto-pause-delay "$SQL_PAUSE_AFTER_MINUTES" -o none \
      && note "Set: serverless, $SQL_MIN_VCORES–$SQL_MAX_VCORES vCore, pauses after $SQL_PAUSE_AFTER_MINUTES min idle." \
      || note "!! Couldn't change it (see the error above). Portal: SQL database > Compute + storage."
  fi
fi

step "Function App: $FUNCTION_APP"
APP_GROUP=$(az functionapp list --query "[?name=='$FUNCTION_APP'].resourceGroup | [0]" -o tsv)
if [ -z "$APP_GROUP" ]; then
  note "Function App $FUNCTION_APP not found: skipped."
else
  az functionapp scale config set -g "$APP_GROUP" -n "$FUNCTION_APP" \
    --instance-memory "$FUNCTION_MEMORY_MB" --maximum-instance-count "$FUNCTION_MAX_INSTANCES" -o none \
    && note "Set: $FUNCTION_MEMORY_MB MB instances, at most $FUNCTION_MAX_INSTANCES." \
    || note "!! Couldn't change it. Portal: Function App > Scale and concurrency."
  READY=$(az functionapp scale config show -g "$APP_GROUP" -n "$FUNCTION_APP" --query "length(alwaysReady || \`[]\`)" -o tsv 2>/dev/null)
  [ "${READY:-0}" != "0" ] && note "!! It has always-ready instances, which bill all the time. Remove them: Scale and concurrency > Always ready."
fi

step "Log cap: $LOG_CAP_GB GB a day"
WORKSPACES=$(az monitor log-analytics workspace list --query "[].[resourceGroup, name]" -o tsv 2>/dev/null)
if [ -z "$WORKSPACES" ]; then
  note "No Log Analytics workspace found: skipped."
else
  while read -r group name; do
    az monitor log-analytics workspace update -g "$group" -n "$name" --quota "$LOG_CAP_GB" -o none \
      && note "$name: capped at $LOG_CAP_GB GB a day." \
      || note "!! $name: couldn't set the cap."
  done <<<"$WORKSPACES"
fi

step "Two things to do in the portals (2 minutes)"
note "1. Azure budget: https://portal.azure.com/#view/Microsoft_Azure_CostManagement/Menu/~/budgets"
note "   + Add > Monthly, US\$20, alerts at 25%, 50% and 100% to your email."
note "2. Firebase budget: https://console.cloud.google.com/billing/budgets (project sidegigphilippines)"
note "   Create budget > US\$5 a month, alerts at 50% and 100%."
note "3. Firebase App Check: Firebase console > App Check > Android app > Play Integrity > Register,"
note "   then Authentication > Settings > turn on App Check enforcement (after a build with App Check is out)."
note "   Philippine-only SMS codes: paste firebase/cloudshell-auth.sh in Google Cloud Shell."

echo
echo "Done."
SIDEGIGPH_COST
