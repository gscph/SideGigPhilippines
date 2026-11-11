# =====================================================================================
# SideGigPh: check that cloudshell-setup.sh worked. READ-ONLY: changes nothing.
#
# How: in Google Cloud Shell (console.cloud.google.com → >_ icon), paste this WHOLE file,
# press Enter. Each line says PASS, WAIT (still building, check again in a few minutes),
# or FAIL (with what to do). A summary is at the end.
#
# Last part: it runs the live security rules against a few pretend requests (signed out,
# signed in, with and without a verified phone) to prove they allow and block what they should.
# =====================================================================================

bash -s <<'SIDEGIGPH_VERIFY'
set -uo pipefail

PROJECT_ID="sidegigphilippines"
LOCATION="asia-southeast1"

RULES_API="https://firebaserules.googleapis.com/v1/projects/$PROJECT_ID"
DOCS="/databases/(default)/documents"
passed=0; waiting=0; failed=0
pass() { printf '  PASS  %s\n' "$1"; passed=$((passed + 1)); }
wait_() { printf '  WAIT  %s\n' "$1"; waiting=$((waiting + 1)); }
fail() { printf '  FAIL  %s\n' "$1"; failed=$((failed + 1)); }
skip() { printf '  SKIP  %s\n' "$1"; }
step() { printf '\n== %s\n' "$1"; }
call() {  # method url [json-body]: a Google API call as you, billed to the project
  local args=(-sS -X "$1" "$2"
    -H "Authorization: Bearer $(gcloud auth print-access-token)"
    -H "x-goog-user-project: $PROJECT_ID"
    -H "Content-Type: application/json")
  if [ $# -ge 3 ]; then args+=(-d "$3"); fi
  curl "${args[@]}"
}

step "Project"
if gcloud config set project "$PROJECT_ID" >/dev/null 2>&1 && gcloud projects describe "$PROJECT_ID" >/dev/null 2>&1; then
  pass "$PROJECT_ID is open"
else
  fail "can't open $PROJECT_ID: sign in to Cloud Shell with the account that owns it"
fi

step "APIs"
enabled=$(gcloud services list --enabled --format="value(config.name)" 2>/dev/null)
for api in firestore.googleapis.com firebaserules.googleapis.com identitytoolkit.googleapis.com; do
  if grep -qx "$api" <<<"$enabled"; then pass "$api on"; else fail "$api is off: run cloudshell-setup.sh"; fi
done

step "Firestore database"
db=$(gcloud firestore databases describe --database="(default)" --format=json 2>/dev/null || true)
if [ -z "$db" ]; then
  fail "no database: run cloudshell-setup.sh"
else
  location=$(jq -r '.locationId' <<<"$db")
  [ "$location" = "$LOCATION" ] && pass "in $location" \
    || fail "it's in $location, not $LOCATION (a location can't be changed; only matters if you didn't choose it)"
  [ "$(jq -r '.type' <<<"$db")" = "FIRESTORE_NATIVE" ] && pass "native mode" \
    || fail "not in native mode ($(jq -r '.type' <<<"$db")): the app needs native Firestore"
fi

# The source of the rules that are live for a release, or nothing.
live_rules() {  # release-id
  local ruleset
  ruleset=$(call GET "$RULES_API/releases/$1" | jq -r '.rulesetName // empty')
  [ -n "$ruleset" ] && call GET "https://firebaserules.googleapis.com/v1/$ruleset" | jq -r '.source.files[0].content // empty'
}

step "Firestore security rules"
FIRESTORE_RULES=$(live_rules "cloud.firestore")
if [ -z "$FIRESTORE_RULES" ]; then
  fail "no rules are live: run cloudshell-setup.sh"
else
  for collection in "/users/{uid}" "/professionals/{uid}" "/reviews/{reviewId}" "/chats/{chatId}" "/messages/{messageId}"; do
    grep -qF "match $collection" <<<"$FIRESTORE_RULES" && pass "rules cover $collection" \
      || fail "rules don't cover $collection: an old version is live, run cloudshell-setup.sh"
  done
fi

step "Indexes"
indexes=$(gcloud firestore indexes composite list --format=json 2>/dev/null || echo "[]")
check_index() {  # collection field1 field2
  local state
  state=$(jq -r --arg c "$1" --arg a "$2" --arg b "$3" '
    [.[] | select((.name | contains("/collectionGroups/" + $c + "/"))
                  and ([.fields[].fieldPath] | index($a) != null and index($b) != null))][0].state // "MISSING"' <<<"$indexes")
  case "$state" in
    READY) pass "$1 by $2 + $3" ;;
    CREATING) wait_ "$1 by $2 + $3 is still building" ;;
    *) fail "$1 by $2 + $3 is $state: run cloudshell-setup.sh" ;;
  esac
}
check_index chats professionalId lastMessageAt
check_index chats seekerUid lastMessageAt
check_index reviews professionalId createdAt
check_index reviews seekerUid createdAt

step "Sign-in settings"
config=$(call GET "https://identitytoolkit.googleapis.com/admin/v2/projects/$PROJECT_ID/config")
if jq -e '.error' >/dev/null <<<"$config"; then
  fail "can't read the Auth settings: $(jq -r '.error.message' <<<"$config")"
else
  jq -e '.smsRegionConfig.allowlistOnly.allowedRegions == ["PH"]' >/dev/null <<<"$config" \
    && pass "SMS codes go to +63 numbers only" \
    || fail "SMS region policy isn't Philippines-only: Firebase console > Authentication > Settings > SMS region policy"
  jq -e '.signIn.phoneNumber.enabled == true' >/dev/null <<<"$config" \
    && pass "phone sign-in is on" \
    || fail "phone sign-in is off: Firebase console > Authentication > Sign-in method > Phone"
fi

step "_meta/schema"
meta=$(call GET "https://firestore.googleapis.com/v1/projects/$PROJECT_ID/databases/(default)/documents/_meta/schema")
jq -e '.fields.version' >/dev/null <<<"$meta" && pass "written" || fail "missing: run cloudshell-setup.sh"

step "Live rules, tried with pretend requests"
if [ -z "$FIRESTORE_RULES" ]; then
  fail "skipped: no Firestore rules are live"
else
  PHONE='{"uid": "ana", "token": {"phone_number": "+639171234567"}}'
  NO_PHONE='{"uid": "ana", "token": {}}'
  tests=$(jq -n --arg docs "$DOCS" --argjson phone "$PHONE" --argjson nophone "$NO_PHONE" '[
    {why: "signed out: cannot read a profile",                 expectation: "DENY",
     request: {auth: null,      path: ($docs + "/users/ana"), method: "get"}},
    {why: "signed in: can read own profile",              expectation: "ALLOW",
     request: {auth: $nophone,  path: ($docs + "/users/ana"), method: "get"}},
    {why: "signed in: cannot read another person profile",    expectation: "DENY",
     request: {auth: $nophone,  path: ($docs + "/users/ben"), method: "get"}},
    {why: "no verified phone: cannot search listings",            expectation: "DENY",
     request: {auth: $nophone,  path: ($docs + "/professionals/ben"), method: "get"}},
    {why: "verified phone: can search listings",          expectation: "ALLOW",
     request: {auth: $phone,    path: ($docs + "/professionals/ben"), method: "get"}},
    {why: "signed out: cannot read a chat",                       expectation: "DENY",
     request: {auth: null,      path: ($docs + "/chats/ben_ana"), method: "get"}},
    {why: "signed in: can check whether her own chat exists yet", expectation: "ALLOW",
     request: {auth: $nophone,  path: ($docs + "/chats/ben_ana"), method: "get"}},
    {why: "signed in: cannot check on a chat between other people", expectation: "DENY",
     request: {auth: $nophone,  path: ($docs + "/chats/ben_cat"), method: "get"}}
  ]')
  body=$(jq -n --arg src "$FIRESTORE_RULES" --argjson cases "$tests" \
    '{source: {files: [{name: "firestore.rules", content: $src}]},
      testSuite: {testCases: [$cases[] | {expectation, request}]}}')
  results=$(call POST "$RULES_API:test" "$body")
  if jq -e '.error' >/dev/null <<<"$results"; then
    skip "couldn't run the rule tests ($(jq -r '.error.message' <<<"$results")); the checks above still count"
  else
    for i in $(seq 0 $(($(jq length <<<"$tests") - 1))); do
      why=$(jq -r ".[$i].why" <<<"$tests")
      [ "$(jq -r ".testResults[$i].state" <<<"$results")" = "SUCCESS" ] && pass "$why" \
        || fail "$why: the rules did the opposite ($(jq -c ".testResults[$i].debugMessages // []" <<<"$results"))"
    done
  fi
fi

printf '\n== Summary: %d passed, %d still building, %d failed\n' "$passed" "$waiting" "$failed"
if [ "$failed" -eq 0 ] && [ "$waiting" -eq 0 ]; then
  echo "Everything is set up."
elif [ "$failed" -eq 0 ]; then
  echo "Set up; indexes are still building. Paste this again in a few minutes."
else
  echo "Fix the FAIL lines (most say: run cloudshell-setup.sh again), then paste this again."
fi
SIDEGIGPH_VERIFY
