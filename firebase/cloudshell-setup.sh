# =====================================================================================
# SideGigPh: Firestore setup, for Google Cloud Shell. (Photos are in Azure: azure/cloudshell-setup.sh.)
#
# How: open https://console.cloud.google.com/ (signed in as the project owner), click the
# Cloud Shell icon (>_ top right), paste this WHOLE file, press Enter. Takes 1–3 minutes.
# Safe to run again: it skips what exists and re-deploys the rules and indexes.
#
# What it does:
#   1. turns on the APIs it needs
#   2. creates the Firestore database in asia-southeast1 (Singapore). PERMANENT: can't move later.
#   3. deploys the security rules (they are also the "schema": allowed fields, types, lengths)
#   4. creates the composite indexes the Inbox and review lists need
#   5. limits SMS sign-in codes to Philippine numbers (blocks SMS-pumping fraud)
#   6. writes _meta/schema, so you can see the database in the console
#
# Firestore has no CREATE TABLE: users, professionals, chats and reviews appear
# by themselves when the app writes its first document to each.
#
# Generated from firebase/firestore.rules and firebase/firestore.indexes.json.
# If those change, regenerate this file.
# =====================================================================================

bash -s <<'SIDEGIGPH_SETUP'
set -uo pipefail

PROJECT_ID="sidegigphilippines"
LOCATION="asia-southeast1"
SMS_ONLY_PHILIPPINES="yes"   # "no" to leave the SMS region policy alone

RULES_API="https://firebaserules.googleapis.com/v1/projects/$PROJECT_ID"
step() { printf '\n== %s\n' "$1"; }
fail() { printf '\n!! STOPPED: %s\n' "$1"; exit 1; }
call() {  # method url [json-body]: a Google API call as you, billed to the project
  local args=(-sS -X "$1" "$2"
    -H "Authorization: Bearer $(gcloud auth print-access-token)"
    -H "x-goog-user-project: $PROJECT_ID"
    -H "Content-Type: application/json")
  if [ $# -ge 3 ]; then args+=(-d "$3"); fi
  curl "${args[@]}"
}

step "Project $PROJECT_ID"
gcloud config set project "$PROJECT_ID" >/dev/null 2>&1 \
  || fail "can't open project $PROJECT_ID. Are you signed in with the account that owns it?"

step "Turning on the APIs (Firestore, Rules, Auth)"
gcloud services enable firestore.googleapis.com firebaserules.googleapis.com \
  identitytoolkit.googleapis.com \
  || fail "couldn't turn on the APIs."

step "Firestore database"
if existing=$(gcloud firestore databases describe --database="(default)" --format="value(locationId)" 2>/dev/null); then
  echo "Already exists, in $existing."
else
  gcloud firestore databases create --database="(default)" --location="$LOCATION" --type=firestore-native \
    || fail "couldn't create the database."
fi

WORK=$(mktemp -d)
cat > "$WORK/firestore.rules" <<'FIRESTORE_RULES'
rules_version = '2';

// SideGigPh — Firestore security rules (MVP). The data model they enforce is docs/data-model.md.
//
// Firestore has no schema and no server in front of it: the phone talks to it directly, so these
// rules ARE the API. They decide who may read/write each document and which fields and values
// are allowed (the "schema"). Anything not matched below is denied.
//
// Everyone signs in first (D1). Searching, contacting and publishing also need a verified phone
// number on the account (D2/D3): a phone sign-in has one; a Google sign-in must link one.
//
// Deploy:   ./setup.ps1 -ProjectId <id>       Check syntax:   ./setup.ps1 -Emulator

service cloud.firestore {
  match /databases/{database}/documents {

    // ---------- helpers ----------

    function signedIn() { return request.auth != null; }
    function isMe(uid) { return signedIn() && request.auth.uid == uid; }
    // The phone number the account verified by SMS code, or '' if none (Google, not linked yet).
    function verifiedPhone() { return request.auth.token.get('phone_number', ''); }
    function hasVerifiedPhone() { return signedIn() && verifiedPhone() != ''; }

    function incoming() { return request.resource.data; }
    function existing() { return resource.data; }
    function changedKeys() { return incoming().diff(existing()).affectedKeys(); }
    function doc(path) { return /databases/$(database)/documents/$(path); }

    function isText(value, maxLength) { return value is string && value.size() <= maxLength; }
    function isName(value) { return value is string && value.size() > 0 && value.size() <= 60; }
    // Optional field: absent, null, or text up to maxLength.
    function optText(data, key, maxLength) {
      return data.get(key, null) == null || isText(data[key], maxLength);
    }
    function optId(data, key) { return optText(data, key, 40); }
    function isArea(value) { return value in ['metro-manila', 'rizal', 'laguna', 'cavite']; }
    // A photo stored by SideGigPh.Api: its SHA-256 id and its URL in Azure Blob Storage, which must
    // be that same photo in our container (no links to other sites). Change both if the account moves.
    function photoUrl(id) {
      return 'https://realtimeordertracker.blob.core.windows.net/photos/' + id + '.jpg';
    }
    function isPhoto(value) {
      return value is map && value.keys().hasOnly(['id', 'url'])
        && value.id is string && value.id.matches('^[0-9a-f]{64}$')
        && value.url == photoUrl(value.id);
    }

    // ---------- users/{uid}: the person, private to them ----------

    match /users/{uid} {
      allow read, delete: if isMe(uid);
      allow create, update: if isMe(uid) && validUser(incoming());

      function validUser(d) {
        return d.keys().hasOnly(['displayName', 'phoneNumber', 'email', 'language', 'role',
                                 'incomeGoalId', 'inquiriesSeenAt', 'createdAt', 'updatedAt'])
          && optText(d, 'displayName', 80)
          // Only the number this account verified.
          && (d.get('phoneNumber', null) == null || d.phoneNumber == verifiedPhone())
          && optText(d, 'email', 254)
          && d.language in ['english', 'taglish']
          && (d.get('role', null) == null || d.role in ['seeker', 'professional'])
          && optId(d, 'incomeGoalId');
      }

      // This person's phones, for chat push notifications (doc id = the push token).
      match /devices/{token} {
        allow read, delete: if isMe(uid);
        allow create, update: if isMe(uid)
          && incoming().keys().hasOnly(['platform', 'updatedAt'])
          && incoming().platform in ['android', 'ios'];
      }
    }

    // ---------- professionals/{uid}: the published Skilled Worker listing ----------

    match /professionals/{uid} {
      allow read: if isMe(uid) || hasVerifiedPhone();

      // Anti-fraud rule 1: the listing's number is the account's verified number. Firebase Auth
      // allows one account per phone number, so one number = one listing.
      allow create, update: if isMe(uid)
        && hasVerifiedPhone()
        && validListing(incoming())
        && incoming().mobileNumber == verifiedPhone();

      allow delete: if isMe(uid);

      function validListing(d) {
        return d.keys().hasOnly(['displayName', 'mobileNumber', 'categoryId', 'serviceIds', 'areaId',
                                 'cityIds', 'experienceId', 'rateTierId', 'profilePhoto',
                                 'workPhotos', 'publishedAt', 'updatedAt'])
          && isName(d.displayName)
          && isText(d.categoryId, 40)
          && d.serviceIds is list && d.serviceIds.size() <= 60
          && isArea(d.areaId)
          && d.cityIds is list && d.cityIds.size() >= 1 && d.cityIds.size() <= 40
          && optId(d, 'experienceId')
          && optId(d, 'rateTierId')
          && (d.get('profilePhoto', null) == null || isPhoto(d.profilePhoto))
          && d.workPhotos is list && d.workPhotos.size() <= 12;
      }
    }

    // ---------- reviews/{proId}_{seekerUid}: one per Seeker per Skilled Worker ----------

    match /reviews/{reviewId} {
      allow read: if hasVerifiedPhone() || (signedIn() && existing().seekerUid == request.auth.uid);

      allow create: if hasVerifiedPhone()
        && reviewId == incoming().professionalId + '_' + request.auth.uid
        && incoming().seekerUid == request.auth.uid
        && incoming().professionalId != request.auth.uid
        && validReview(incoming())
        // "Only contacted Skilled Workers can be reviewed": their chat exists.
        && exists(doc('chats/' + reviewId));

      // The reviewer edits their own review. The Skilled Worker can't edit or delete it.
      allow update: if signedIn()
        && existing().seekerUid == request.auth.uid
        && validReview(incoming())
        && !changedKeys().hasAny(['professionalId', 'seekerUid', 'createdAt']);

      allow delete: if signedIn() && existing().seekerUid == request.auth.uid;

      function validReview(d) {
        return d.keys().hasOnly(['professionalId', 'seekerUid', 'reviewerName', 'rating',
                                 'comment', 'serviceId', 'createdAt', 'updatedAt'])
          && d.rating is int && d.rating >= 1 && d.rating <= 5
          && isName(d.reviewerName)
          && optText(d, 'comment', 1000)
          && optId(d, 'serviceId');
      }
    }

    // ---------- chats/{proId}_{seekerUid}: one chat per Seeker per Skilled Worker ----------

    match /chats/{chatId} {
      function isParticipant(chat) {
        return signedIn()
          && (chat.seekerUid == request.auth.uid || chat.professionalId == request.auth.uid);
      }

      // A chat id is {professionalId}_{seekerUid}, so it says whose chat it would be.
      function isMyChatId() {
        return signedIn()
          && (chatId.matches(request.auth.uid + '_.*') || chatId.matches('.*_' + request.auth.uid));
      }

      // Inbox queries must filter on  seekerUid == me  or  professionalId == me.
      allow list: if isParticipant(existing());

      // The app reads a chat before its first message, to see if it's there yet. For a chat that
      // doesn't exist, "not found" is allowed to the two people it would be between (and no one else).
      allow get: if isParticipant(existing()) || (resource == null && isMyChatId());

      // The Seeker starts it with their first message (same batch), to a published listing.
      allow create: if hasVerifiedPhone()
        && chatId == incoming().professionalId + '_' + request.auth.uid
        && incoming().seekerUid == request.auth.uid
        && incoming().professionalId != request.auth.uid
        && validChat(incoming())
        && exists(doc('professionals/' + incoming().professionalId));

      // Either person: a new last message (with its message), or their own read time.
      allow update: if isParticipant(existing())
        && validChat(incoming())
        && (
          (changedKeys().hasOnly(['lastMessage', 'lastMessageAt',
                                  existing().seekerUid == request.auth.uid ? 'seekerReadAt' : 'professionalReadAt'])
            && incoming().lastMessage.senderUid == request.auth.uid
            && existsAfter(doc('chats/' + chatId + '/messages/' + incoming().lastMessage.id)))
          || (existing().seekerUid == request.auth.uid && changedKeys().hasOnly(['seekerReadAt']))
          || (existing().professionalId == request.auth.uid && changedKeys().hasOnly(['professionalReadAt']))
        );

      // Start over / delete account removes this person's chats.
      allow delete: if isParticipant(existing());

      function validChat(d) {
        return d.keys().hasOnly(['professionalId', 'seekerUid', 'professionalName', 'seekerName',
                                 'serviceId', 'cityId', 'urgencyId', 'budgetId',
                                 'lastMessage', 'lastMessageAt', 'seekerReadAt', 'professionalReadAt',
                                 'createdAt'])
          && isName(d.professionalName)
          && isName(d.seekerName)
          && optId(d, 'serviceId') && optId(d, 'cityId')
          && optId(d, 'urgencyId') && optId(d, 'budgetId')
          && d.lastMessage is map
          && d.lastMessage.keys().hasOnly(['id', 'text', 'kind', 'senderUid', 'at'])
          && d.lastMessageAt is timestamp;
      }

      match /messages/{messageId} {
        // Read through the parent chat (a get per request, cached across a query). Before the first
        // message there is no chat yet: the two people it would be between see an empty list.
        allow read: if exists(doc('chats/' + chatId))
          ? isParticipant(get(doc('chats/' + chatId)).data)
          : isMyChatId();
        allow delete: if isParticipant(get(doc('chats/' + chatId)).data);

        // Works for the first message too: getAfter sees the chat created in the same batch.
        allow create: if isParticipant(getAfter(doc('chats/' + chatId)).data)
          && incoming().keys().hasOnly(['senderUid', 'kind', 'text', 'at'])
          && incoming().senderUid == request.auth.uid
          && incoming().at is timestamp
          && (
            (incoming().kind == 'text' && incoming().text is string
              && incoming().text.size() > 0 && incoming().text.size() <= 1000)
            || (incoming().kind == 'call' && incoming().get('text', null) == null)
          );
      }
    }

    // Photos are not in Firebase: SideGigPh.Api keeps them in Azure Blob Storage and enforces
    // anti-fraud rule 2 (one photo, one account) itself. Listings store only {id, url}.
  }
}
FIRESTORE_RULES

# Uploads a rules file as a new ruleset, then points the release at it (creates it the first time).
deploy_rules() {  # file release-id
  local response ruleset release body
  body=$(jq -n --rawfile src "$1" --arg name "$(basename "$1")" '{source: {files: [{name: $name, content: $src}]}}')
  response=$(call POST "$RULES_API/rulesets" "$body")
  ruleset=$(jq -r '.name // empty' <<<"$response")
  [ -n "$ruleset" ] || { echo "$response"; fail "$(basename "$1") was rejected (the error above has the line and column)."; }

  release="projects/$PROJECT_ID/releases/$2"
  body=$(jq -n --arg r "$release" --arg s "$ruleset" '{name: $r, rulesetName: $s}')
  response=$(call POST "$RULES_API/releases" "$body")
  if jq -e '.error.code == 409' >/dev/null <<<"$response"; then
    response=$(call PATCH "$RULES_API/releases/$2" "$(jq -n --argjson rel "$body" '{release: $rel}')")
  fi
  jq -e '.error' >/dev/null <<<"$response" && { echo "$response"; fail "couldn't release $2."; }
  echo "Live: $(basename "$1")"
}

step "Firestore security rules"
deploy_rules "$WORK/firestore.rules" "cloud.firestore"

step "Indexes (they build in the background for a few minutes)"
create_index() {  # collection field-config...
  local collection=$1 out args=()
  shift
  for field in "$@"; do args+=(--field-config="$field"); done
  if out=$(gcloud firestore indexes composite create --collection-group="$collection" \
      --query-scope=collection "${args[@]}" --async 2>&1); then
    echo "$collection ($*): building"
  elif grep -qi "already exists" <<<"$out"; then
    echo "$collection ($*): already there"
  else
    echo "$out"; fail "couldn't create an index on $collection."
  fi
}
create_index chats   field-path=professionalId,order=ascending field-path=lastMessageAt,order=descending
create_index chats   field-path=seekerUid,order=ascending      field-path=lastMessageAt,order=descending
create_index reviews field-path=professionalId,order=ascending field-path=createdAt,order=descending
create_index reviews field-path=seekerUid,order=ascending      field-path=createdAt,order=descending

step "SMS sign-in codes: Philippine numbers only"
if [ "$SMS_ONLY_PHILIPPINES" = "yes" ]; then
  response=$(call PATCH "https://identitytoolkit.googleapis.com/admin/v2/projects/$PROJECT_ID/config?updateMask=smsRegionConfig" \
    '{"smsRegionConfig": {"allowlistOnly": {"allowedRegions": ["PH"]}}}')
  if jq -e '.error' >/dev/null <<<"$response"; then
    echo "$response"
    echo "Warning: not changed. Set it by hand: Firebase console > Authentication > Settings > SMS region policy."
  else
    echo "Only +63 numbers can get a code."
  fi
else
  echo "Skipped (SMS_ONLY_PHILIPPINES is not yes)."
fi

step "_meta/schema (so the database shows something in the console)"
now=$(date -u +%Y-%m-%dT%H:%M:%SZ)
response=$(call PATCH "https://firestore.googleapis.com/v1/projects/$PROJECT_ID/databases/(default)/documents/_meta/schema" \
  "$(jq -n --arg now "$now" '{fields: {
      version: {integerValue: "1"},
      collections: {arrayValue: {values: [
        {stringValue: "users"}, {stringValue: "professionals"}, {stringValue: "chats"},
        {stringValue: "reviews"}]}},
      plan: {stringValue: "docs/data-model.md in the SideGigPh repo"},
      updatedAt: {timestampValue: $now}}}')")
jq -e '.error' >/dev/null <<<"$response" && { echo "$response"; fail "couldn't write _meta/schema."; }
echo "Written."

rm -rf "$WORK"
step "Done"
echo "Firestore: https://console.firebase.google.com/project/$PROJECT_ID/firestore"
echo "Indexes:   https://console.firebase.google.com/project/$PROJECT_ID/firestore/indexes"
SIDEGIGPH_SETUP
