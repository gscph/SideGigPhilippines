# =====================================================================================
# SideGigPh: Firebase Authentication settings, for Google Cloud Shell. Firebase is only the
# sign-in provider; the data is in Azure SQL (SideGigPh.AzureSQL/Sql/) behind SideGigPh.Api.
#
# How: open https://console.cloud.google.com/ (signed in as the project owner), click the
# Cloud Shell icon (>_ top right), paste this WHOLE file, press Enter. Safe to run again.
#
# What it does:
#   1. turns on the Identity Toolkit API (Firebase Auth's admin API)
#   2. turns on the Play Integrity API (Android phone sign-in checks the app with it; without it
#      sending a code fails with 17028 "Invalid app info in play_integrity_token")
#   3. limits SMS sign-in codes to Philippine numbers (blocks SMS-pumping fraud)
#
# Turning on the Phone and Google providers, the Blaze plan and the app's SHA-1/SHA-256 stay
# in the Firebase console: see firebase/README.md.
# =====================================================================================

bash -s <<'SIDEGIGPH_AUTH'
set -uo pipefail

PROJECT_ID="sidegigphilippines"

step() { printf '\n== %s\n' "$1"; }
fail() { printf '\n!! STOPPED: %s\n' "$1"; exit 1; }

step "Project $PROJECT_ID"
gcloud config set project "$PROJECT_ID" >/dev/null 2>&1 \
  || fail "can't open project $PROJECT_ID. Are you signed in with the account that owns it?"

step "Turning on the Auth admin API"
gcloud services enable identitytoolkit.googleapis.com || fail "couldn't turn on identitytoolkit.googleapis.com."

step "Turning on the Play Integrity API (Android phone sign-in)"
gcloud services enable playintegrity.googleapis.com || fail "couldn't turn on playintegrity.googleapis.com."

step "SMS sign-in codes: Philippine numbers only"
response=$(curl -sS -X PATCH \
  "https://identitytoolkit.googleapis.com/admin/v2/projects/$PROJECT_ID/config?updateMask=smsRegionConfig" \
  -H "Authorization: Bearer $(gcloud auth print-access-token)" \
  -H "x-goog-user-project: $PROJECT_ID" \
  -H "Content-Type: application/json" \
  -d '{"smsRegionConfig": {"allowlistOnly": {"allowedRegions": ["PH"]}}}')
if jq -e '.error' >/dev/null <<<"$response"; then
  echo "$response"
  fail "not changed. Set it by hand: Firebase console > Authentication > Settings > SMS region policy."
fi
echo "Only +63 numbers can get a code."

step "Done"
SIDEGIGPH_AUTH
