# SideGigPh — Persistent data plan (MVP: Firestore, swappable to SQL later)

> **Status:** the collections, rules and the app's Firestore adapter follow this plan (build order, §9). Push notifications and Delete account are still to do.
> Setup files that match this plan: [`firebase/`](../firebase/README.md) (security rules, indexes, `setup.ps1`) and [`azure/`](../azure/README.md) (photo storage + `SideGigPh.Api`).
>
> **MVP rule:** only what's needed for a Seeker to find, chat with and review a Skilled Worker, and for a Skilled Worker to publish a listing and answer chats. Everything else waits for alpha feedback (§8).

## 1. Decisions (owner, 2026-11-11)

| # | Decision |
|---|---|
| D1 | **Everyone signs in before using the app.** Every Firestore read and write requires a signed-in user. |
| D2 | **Phone number + SMS code is the main sign-in.** A successful code means the number is verified. **Sign in with Google** sits below it as the second option. |
| D3 | A phone sign-in never asks for the number again. A **Google** sign-in gives first and last name (never asked again) but **must add and verify a phone number** before it can **search** (Seeker) or **publish** (Skilled Worker). |
| D4 | **In-app chat.** Both sides' Inbox is a list of chats; a Skilled Worker answers inside the app. **Call** still opens the phone's dialer and is logged in the chat. In-app chat replaces "Send text" (SMS). |
| — | No GCash. No businesses (org accounts), ID verification, reports or search logs in the MVP. |

## 2. Sign-in flow (replaces "auth after the interview")

```
/            LaunchRouter
/start       Language (English / Taglish)      ← needed so the sign-in screen is in their language
/verify      Sign in: [Phone number + code]    ← primary, big
                      [Sign in with Google]    ← secondary, below
/verify/name First + last name                 ← ONLY after a phone sign-in (Google already gave it)
/start/role  Seeker / Skilled Worker
…interview…  (Skilled Worker: the "name" and "number" steps are removed — both come from the account)
             Google account with no phone yet:
               /verify/phone  (phone + code, linked to the same account)
               Seeker → before the first search results
               Skilled Worker → before Publish
```

- **Returning user on a new phone:** sign in → `users/{uid}` has their role → a Skilled Worker's listing is loaded from `professionals/{uid}` → straight to home. No interview again.
- **Linking a phone to a Google account** uses the provider's "link credential" call, so it's the same account (same uid), not a second one.
- If that number already belongs to another account, linking fails → "This number is already used by another account. Sign in with your number instead."
- After linking, the app must refresh the ID token so the new phone number reaches Firestore's rules.

### Auth contract (built, step 2)

| Change | Why |
|---|---|
| `IIdentityProvider.LinkPhoneAsync(verificationId, code)` (the code is sent with the existing `SignUpWithPhoneAsync`) | Google users add a verified number (D3) |
| `IIdentityProvider.SetDisplayNameAsync(name)` | Saves the name typed after a phone sign-in on the account itself |
| `AuthError.PhoneInUse` | That number is already used by another account |
| `AuthUser.HasName`, `AuthUser.HasVerifiedPhone`; `PhoneNumber` now filled by the Firebase adapter | The gate checks |
| `SignInGate`, `AuthRoutes`, `NamePage` (`/verify/name`), `AddPhonePage` (`/verify/phone`) | Every page asks the gate; it returns the missing step (sign in, name, phone) |

Google gives one full name, not first and last separately, so the account keeps a single display name. Short forms (reviews show "Maricel D.") come from `PersonName.Short`.

## 3. How it stays swappable

- **Pages and workspaces only talk to ports** (`IProfessionalProfileRepository`, `IReviewRepository`, new `IChatRepository` and `IUserRepository`, `IPhotoStore`). That's the swap point, same as today's `Dev/` stubs.
- **The Firestore adapter lives in `SideGigPh.Maui/Services/Firestore/`**, the only folder allowed to reference the Firestore SDK.
  - Its document classes (`ProfessionalDoc`…, with Firestore attributes) and mappers stay in that folder. Models in `Models/` never get Firestore attributes or types (`Timestamp`, `DocumentReference`).
  - Moving the adapter into its own project later is a mechanical move.
- **One line in `MauiProgram`** picks the stubs or Firestore.
- **The SQL path:** the phone never connects to a database (the password would ship inside the app). Later: `Services/Api/` (an `HttpClient` adapter) → Azure Functions → EF Core → PostgreSQL / MySQL / SQL Server.
- **Swap-friendly habits:**
  - String ids: Firebase uid, SHA-256 hex, `{proId}_{seekerUid}`.
  - UTC `DateTimeOffset` in models.
  - No denormalized copies that need a server to keep them in sync.

## 4. Collections (4)

🔒 owner only · ✅ any signed-in user **with a verified phone**

### 4.1 `users/{uid}` 🔒: the person (Seeker or Skilled Worker)

| Field | Type | Notes |
|---|---|---|
| `displayName` | string | Full name: from Google, or the name screen after a phone sign-in (same as the auth account) |
| `phoneNumber` | string? | E.164, must equal the verified number on the account |
| `email` | string? | From Google |
| `language` | string | `english` / `taglish` |
| `role` | string? | `seeker` / `professional` (set at the role fork) |
| `incomeGoalId` | string? | Skilled Worker's private monthly goal (never public) |
| `createdAt`, `updatedAt` | timestamp | |

The interview answers stay on the phone (`IOnboardingStore`, as today). A Seeker's search answers aren't stored online in the MVP.
**SQL:** `users` table, `UNIQUE(phone_number)`.

### 4.2 `professionals/{uid}` ✅ read · 🔒 write: the published Skilled Worker listing

The doc id is the owner's uid. The doc exists only while the listing is published: Start over / delete = delete the doc.

| Field | Type | Notes |
|---|---|---|
| `displayName` | string | The account name (`users.displayName`) |
| `mobileNumber` | string | **= the account's verified phone** (rules enforce). This is anti-fraud rule 1. |
| `categoryId` | string | The one trade (`ProfessionalDraft.CategoryId`) |
| `serviceIds` | string[] | Jobs they do |
| `areaId` | string | Metro Manila / Rizal / Laguna / Cavite |
| `cityIds` | string[] | Towns they go to, inside `areaId` |
| `experienceId`, `rateTierId` | string? | `ReferenceData` ids |
| `profilePhoto` | map? | `{id, url}` |
| `workPhotos` | map[] | ≤ 12 × `{id, url}`, newest first. Always read with the listing, so it's a list here, not a separate collection |
| `publishedAt`, `updatedAt` | timestamp | |

**SQL:** `professionals` + child tables `professional_services`, `professional_cities`, `professional_photos`.

### 4.3 `reviews/{proId}_{seekerUid}` ✅: one per Seeker per Skilled Worker

| Field | Type | Notes |
|---|---|---|
| `professionalId`, `seekerUid` | string | Must match the doc id |
| `reviewerName` | string | "Maricel D.", from the Seeker's name |
| `rating` | int | 1–5 |
| `comment` | string? | ≤ 1000 chars |
| `serviceId` | string? | |
| `createdAt`, `updatedAt` | timestamp | |

- **Only after a chat:** the rules allow a review only if the chat `chats/{proId}_{seekerUid}` exists.
- **No review photos in the MVP.**
- **Star averages for search:** `GetSummariesAsync` (unchanged port) reads the reviews of the workers in the results (`professionalId in [...]`, 30 at a time) and averages them on the phone. No server code needed. When volumes grow, a Cloud Function keeps `ratingAvg`/`ratingCount` on the listing instead.
- **A Skilled Worker can't delete reviews about them** (or they'd wipe bad ones). If they Start over, the reviews stay hidden with the listing and come back if the same account publishes again.

**SQL:** `reviews`, `UNIQUE(professional_id, seeker_uid)`, FK to `chats`.

### 4.4 `chats/{proId}_{seekerUid}`: one chat per Seeker per Skilled Worker (replaces `inquiries`)

Only these two people can read or write it. The doc is one row in each side's Inbox.

| Field | Type | Notes |
|---|---|---|
| `professionalId`, `seekerUid` | string | Must match the doc id |
| `professionalName`, `seekerName` | string | So the Inbox list needs no extra reads |
| `serviceId`, `cityId`, `urgencyId`, `budgetId` | string? | The job, from the Seeker's answers at the first contact. Shown at the top of the chat. |
| `lastMessage` | map | `{id, text, kind, senderUid, at}`: the Inbox preview |
| `lastMessageAt` | timestamp | Inbox order (newest first) |
| `seekerReadAt`, `professionalReadAt` | timestamp? | Unread = `lastMessageAt` is newer than my read time and I didn't send it. That's the Inbox badge; no counters to keep in sync. |
| `createdAt` | timestamp | |

#### `chats/{chatId}/messages/{autoId}`

| Field | Type | Notes |
|---|---|---|
| `senderUid` | string | One of the two people |
| `kind` | string | `text` · `call` (a "📞 Called" line, logged when Call opens the dialer) |
| `text` | string? | ≤ 1000 chars, required for `text` |
| `at` | timestamp | |

- **Sending a message** writes the message and updates the chat's `lastMessage`/`lastMessageAt` in one batch.
- **The first message** is the pre-written one (job, town, urgency), as today's SMS. It creates the chat.
- **Text only in the MVP**, no photos in chat.

**SQL:** `chats` with `UNIQUE(professional_id, seeker_uid)` + `chat_messages(chat_id, sender_uid, kind, text, at)`, index `(chat_id, at)`.

#### Push notifications: needed for chat to work

A message nobody sees until they happen to open the app isn't a chat. Today's SMS reaches the worker's phone on its own; an in-app message only does that with a push notification.

- `users/{uid}/devices/{token}` 🔒 `{platform, updatedAt}`: the phone's push token (Firebase Cloud Messaging).
- **One Cloud Function** (`firebase/functions/`, TypeScript, ~40 lines): when a message is created, send a push to the other person's devices ("Maricel: Hi po, magkano linis ng split-type?"). Tapping it opens the chat. This is the MVP's **only server code**.
- App: `Plugin.Firebase.CloudMessaging` 3.x (asks for notification permission, which Android 13+ requires) behind a skeleton `IPushNotifications` port.
- **SQL path:** the API sends the push after saving the message.

### What's *not* a collection, and why

| Asked about | MVP answer |
|---|---|
| Separate tables for Skilled Worker (individual) / Account (org) / Client | One `users` per person + one `professionals` listing. Orgs come later (§8). |
| A search table / geo search | Not needed. Search = `professionals where areaId == X and categoryId == Y`, then the existing `SearchRanker` on the phone. Towns are the location unit, so no GPS is stored. |
| Phone uniqueness table | Not needed. Firebase Auth already allows one account per phone number, and the rules require `mobileNumber` = that account's verified number. |
| Review photos | Later. |

## 5. Photos (Azure Blob Storage, through `SideGigPh.Api`)

Owner decision (2026-11-11): photos go to **Azure Blob Storage**, not Firebase Storage.

- **Where:** SideGigPh's storage account `sidegigphbfb9` (also the Function App's own storage; only the `photos` container is public), container `photos`, so a photo is `https://sidegigphbfb9.blob.core.windows.net/photos/{sha256}.jpg`. The API builds the URL from its connection string; the Firestore rules accept only that URL for a photo id. Blob: `{sha256}.jpg` (work photos and the profile photo), JPEG ≤ 2 MB (the picker already resizes to 1280px). Set up by [`azure/cloudshell-setup.sh`](../azure/cloudshell-setup.sh).
- **Who reads:** anyone with the exact URL (the container is "Blob" level: readable by URL, not listable). The id is a SHA-256, so URLs can't be guessed, and every photo is on a public listing anyway. Revisit if private photos (ID cards) ever arrive.
- **Who writes:** only `SideGigPh.Api` (Azure Functions), which holds the storage connection string in its settings (`PhotoStorage`). **The phone never has the key.** The app sends the user's Firebase ID token; the API checks it against Firebase's public keys.
  - `POST /api/photos` (body: a JPEG, PNG or iPhone HEIC, ≤ 10 MB; the type is read from the bytes, not the Content-Type): `PhotoImage` turns PNG and HEIC (and a JPEG over 2 MB) into a JPEG of at most 1280px, upright, with a white background behind transparency and EXIF/GPS stripped; a small JPEG is kept byte-for-byte so its id matches the app's. The API creates the `photos` container on the first upload if it's missing, then hashes the stored bytes itself, stores `{sha}.jpg` with `owner = uid` in the blob's metadata, and returns `{id, url}`. The same account sending the same photo again gets the same `{id, url}`. **Anti-fraud rule 2:** another account sending a photo that's already stored gets `409 photo-in-use`.
  - `DELETE /api/photos/{id}`: the owner only.
- **Then** the app saves `{id, url}` into the listing's `workPhotos` / `profilePhoto` on the next publish (the Firestore rules require an `https://` URL).
- **Adapter (step 5, done):** `ApiPhotoStore` (in `Services/Api/`) implements the skeleton's `IPhotoStore` with an `HttpClient`. It sends the sign-in token (`IIdentityProvider.GetIdTokenAsync`, added to the skeleton) and `SaveAsync` throws `PhotoStoreException` when the upload fails, including `409 photo-in-use`. Photos are shown from `https://sidegigphbfb9.blob.core.windows.net/photos/{id}.jpg`. The API address, `https://sidegigphapi-hrb9bgerbgcgawbm.southeastasia-01.azurewebsites.net/api/`, is a constant in `MauiProgram`, like `GoogleWebClientId`: not a secret.
- **SQL path:** the same API grows the other endpoints; photos stay in Blob Storage, with `UNIQUE(sha256)` on `professional_photos`.

## 6. Packages: consuming Firestore from .NET

| Package | Where | Why |
|---|---|---|
| `Plugin.Firebase.Firestore` 3.1.3 | `SideGigPh.Maui` | Wraps the native Android/iOS Firestore SDKs: offline cache, and every request carries the signed-in user so the rules apply |
| `Plugin.Firebase.CloudMessaging` 3.x | `MobileAppTheme.Firebase` | Chat push notifications |

- **That's all.** The existing `google-services.json` / `GoogleService-Info.plist` already cover Firestore. Photos need no package: `ApiPhotoStore` uses `HttpClient` (§5).
- Keep every `Plugin.Firebase.*` package on the **3.x** line, the same as Auth (3.1.1). 4.0 exists, but they share native bindings and must be upgraded together, later.
- **Not `Google.Cloud.Firestore`:** that's the server/admin SDK (service-account keys, bypasses the rules). It never goes in the app.
- **Firestore is schemaless**, so there's no create-table step. A collection appears with its first document. The "schema" is the security rules (allowed fields, types, lengths) in [`firebase/firestore.rules`](../firebase/firestore.rules).

Shape of the adapter code (only inside `Services/Firestore/`):

```csharp
public sealed class ProfessionalDoc : IFirestoreObject
{
    [FirestoreDocumentId] public string Id { get; set; } = "";
    [FirestoreProperty("displayName")] public string DisplayName { get; set; } = "";
    [FirestoreProperty("cityIds")] public IList<string> CityIds { get; set; } = [];
    // …
}

var snapshot = await firestore.GetCollection("professionals")
    .WhereEqualsTo("areaId", query.AreaId)
    .WhereEqualsTo("categoryId", query.CategoryId)
    .GetDocumentsAsync<ProfessionalDoc>();
return snapshot.Documents.Select(d => d.Data.ToListing()).ToList();
```

## 7. Port changes (MVP)

| Port / model | Change |
|---|---|
| `IUserRepository` | **New:** `GetAsync(uid)`, `SaveAsync(UserProfile)`, `DeleteAsync(uid)`. Stub + Firestore adapter. |
| `ProfessionalSearchQuery` | `(CategoryId, AreaId)` instead of `(CategoryId, CityId)` (the ranker already splits by town) |
| `IProfessionalProfileRepository` | Unchanged methods. `PublishResult.PhoneTaken` stays: Firestore can't produce it (the number is the account's verified number), but a SQL backend returns it on a duplicate number. |
| `IReviewRepository` | Unchanged. Firestore implementation. |
| `IChatRepository` (replaces `IInquiryRepository`) | `GetInboxAsync(personId, side)` (chats, newest first), `GetAsync(chatId)`, `GetMessagesAsync(chatId)`, `Watch(chatId, changed)` (live updates while the chat is open), `SendAsync(chat, message)` (creates the chat on the first message), `MarkReadAsync(chatId, side)`, `DeleteForPersonAsync(personId)`. Models: `Chat`, `ChatMessage` replace `Inquiry` / `InboxThread`. |
| `IPushNotifications` (skeleton) | **New:** ask permission, register the device token, open the chat when a push is tapped |
| `IPhotoStore` (skeleton) | Report "used by another account"; `ApiPhotoStore` adapter (§5) |
| `IIdentityProvider` (skeleton) | Phone linking + names (§2) |
| Local ids | `"own"` / `"me"` → the signed-in uid |
| Start over | Becomes **Delete account**: delete my photos (`DELETE /api/photos/{id}`), listing, my chats and their messages, my device tokens, reviews I wrote, my `users` doc, then the auth account. Google Play requires in-app account deletion. |

## 8. Later (alpha feedback decides)

Businesses / org accounts · photos in chat · block a user · review photos · a Skilled Worker's reply to a review · "available today" toggle · saved workers · report a worker or review · search logs (unknown words, demand per town) · ID verification badge · star averages stored on the listing (Cloud Function) · a pre-built search index if read costs grow · perceptual hash for edited copies of photos · GCash · moving the adapter into its own project.

## 9. Build order

1. Create the Firebase database, rules and indexes (`firebase/setup.ps1`); try them in the emulator.
2. **Done:** auth contract changes + the sign-in flow (§2). Update `CLAUDE.md` and `docs/onboarding.md`, since they still say "auth after the interview".
3. **Done:** `IUserRepository` + stub; search by area.
4. **Done:** Chat: `IChatRepository` + stub, chat page for both sides (replaced the SMS "Send text"), Inbox unread badges.
5. **Done:** `Services/Firestore/` adapter for users, listings, reviews and chats; one DI call, `AddFirestoreData()` (build with `-p:UseStubData=true` for the stubs). Photos are Azure's (`ApiPhotoStore`, §5), not part of this step. Run `firebase/cloudshell-setup.sh` first, so the database, rules and indexes exist.
6. Push: `IPushNotifications`, device tokens, the one Cloud Function.
7. **Half done:** `"own"` / `"me"` are replaced by the uid (`ProWorkspace.MyId`, `SeekerWorkspace.MyId`), needed by step 5. Still to do: Start over → Delete account (also the auth account itself).
8. Update `CLAUDE.md` and the `ports-and-stubs` skill.
