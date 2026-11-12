# SideGigPh — Persistent data (Azure SQL behind SideGigPh.Api)

> **Status:** users, listings, reviews and chats live in **Azure SQL**. The app reaches them only through **`SideGigPh.Api`** (Azure Functions), which checks each request's **Firebase ID token**. Firebase stays the sign-in provider. Photos are in Azure Blob Storage (§5).
> Setup: [`azure/`](../azure/README.md) (Function App, database, storage, deploy) and [`firebase/`](../firebase/README.md) (sign-in only). Schema and sample data: [`SideGigPh.AzureSQL/Sql/`](../SideGigPh.AzureSQL/Sql/).
>
> **MVP rule:** keep only what a Seeker needs to find, chat with and review a Skilled Worker, and what a Skilled Worker needs to publish a listing and answer chats. Everything else waits for alpha feedback (§7).

## 1. Decisions

| # | Decision |
|---|---|
| D1 | **Everyone signs in before using the app.** Every API call needs a valid Firebase ID token. |
| D2 | **Phone number + SMS code is the main sign-in.** A successful code means the number is verified. **Sign in with Google** sits below it as the second option. |
| D3 | A phone sign-in never asks for the number again. A **Google** sign-in gives the name (never asked again) but **must add and verify a phone number** before it can **search** (Seeker) or **publish** (Skilled Worker). |
| D4 | **In-app chat.** Both sides' Inbox is a list of chats; a Skilled Worker answers inside the app. **Call** still opens the phone's dialer and is logged in the chat. |
| D5 (2026-11-12) | **Azure SQL replaces Firestore.** The phone never connects to the database (the password would ship inside the app). EF Core 10 on the server. The T-SQL scripts are the schema; there are no EF migrations. |
| — | No GCash. No businesses (org accounts), ID verification, reports or search logs in the MVP. |

## 2. Sign-in flow

```
/            LaunchRouter
/start       Language (English / Taglish)      ← so the sign-in screen is in their language
/verify      Sign in: [Phone number + code]    ← primary, big
                      [Sign in with Google]    ← secondary, below
/verify/name Full name                         ← ONLY after a phone sign-in (Google already gave it)
/start/role  Seeker / Skilled Worker
…interview…  (name and number come from the account, never asked)
             Google account with no phone yet:
               /verify/phone  (phone + code, linked to the same account)
               Seeker → before the first search results
               Skilled Worker → before Publish
```

- **Linking a phone to a Google account** uses the provider's "link credential" call, so it stays the same account (same uid).
- After linking, the app refreshes the ID token so the new `phone_number` claim reaches the API (publishing needs it).
- Google gives one full name, so the account keeps a single display name. Short forms (reviews show "Maricel D.") come from `PersonName.Short`.

## 3. How it fits together

```
Razor page → ProWorkspace / SeekerWorkspace → port (IProfessionalProfileRepository, IReviewRepository,
  IChatRepository, IUserRepository, IPhotoStore) → Services/Api/ adapter (ApiClient + Firebase ID token)
  → HTTPS → SideGigPh.Api function → store (SideGigPh.AzureSQL, EF Core) → Azure SQL
```

| Project | Holds |
|---|---|
| `SideGigPh.Contracts` | The JSON records both sides use (`ListingDto`, `ChatDto`, `PublishListingRequest`…), plus the source-generated `ApiJson`. No dependencies. |
| `SideGigPh.AzureSQL` | `Sql/001-schema.sql`, `Sql/002-seed.sql`, the `*Row` classes, `SideGigPhDb` (mapping) and the stores (`UserStore`, `ProfessionalStore`, `ReviewStore`, `ChatStore`), which hold the data rules. |
| `SideGigPh.Api` | One function class per area (`UserFunctions`, `ProfessionalFunctions`, `ReviewFunctions`, `ChatFunctions`, `PhotoFunctions`). Each checks the token first, then calls a store. |
| `SideGigPh.Maui/Services/Api/` | `ApiClient`, `Api*Repository` adapters, `ApiPhotoStore`, `ApiMapping` (contracts ↔ `Models/`). Registered by `AddSideGigPhApi()`. |

- **Reads that fail** (offline, server error) are logged and come back empty; the screens already have empty states. **Writes** log and throw, and the pages say "didn't send / didn't publish".
- **The server always takes the uid from the token**, never from the body. A person's own data sits under `…/me` / `…/mine` routes.
- **Swapping the database again** (PostgreSQL, PostGIS…) touches only `SideGigPh.AzureSQL`: the API's contracts and the app don't change.

## 4. Tables

All created by `001-schema.sql`. Ids are `varchar`: Firebase uids, `ReferenceData` ids (`aircon`, `aircon-cleaning`, `rizal`, `taytay`, `500-1000`…), and SHA-256 hex for photos. `ReferenceData` ids are stable, so there are no lookup tables. Times are `datetimeoffset(3)` UTC.

| Table | Key | What | Notes |
|---|---|---|---|
| `Users` | `Uid` | The person: name, verified phone, email, `language` (`english`/`taglish`), `role` (`seeker`/`professional`), private `IncomeGoalId`, `ProfilePhotoId` (the account's one profile photo, unique: `UX_Users_ProfilePhoto`) | Only that person reads or writes it (`users/me`). The interview answers stay on the phone (`IOnboardingStore`). |
| `Professionals` | `Id` = uid | The published listing: name, `MobileNumber`, one `CategoryId`, one `AreaId`, `ExperienceId`, `RateTierId`, `ProfilePhotoId`, `PublishedAt` (first went live), `UpdatedAt` | **`UQ_Professionals_MobileNumber`** (anti-fraud rule 1); the number is the token's verified `phone_number`. `IX_Professionals_Category_Area` serves search. The row exists only while published. |
| `ProfessionalServices` | (`ProfessionalId`, `ServiceId`) | The jobs they do | Cascade delete with the listing |
| `ProfessionalCities` | (`ProfessionalId`, `CityId`) | Towns they go to, inside the area | Cascade delete |
| `ProfessionalWorkPhotos` | `PhotoId` | 2–6 work photos (the check allows 12), `Position` 0 = newest | **The PK is global** (anti-fraud rule 2), as is `UX_Professionals_ProfilePhoto`. URLs are built from the id. |
| `ProfessionalReferences` | (`ProfessionalId`, `Position`) | 1–3 people who vouch for the worker: `Name`, `MobileNumber` (+639…) | **Private**: only the count is in `ListingDto` (`ReferenceCount`, one `GROUP BY` like the stars), never names or numbers. Replaced on every publish; cascades with the listing. |
| `Reviews` | (`ProfessionalId`, `SeekerId`) | One per Seeker per worker: `ReviewerName`, `Rating` 1–5, `Comment` ≤ 1000, `ServiceId` | Only after a chat between the two exists. **No FK to `Professionals`**, so a worker who starts over can't wipe their reviews. Averages come from one `GROUP BY` and travel on each listing (`ListingDto.Reviews`). |
| `Chats` | `Id` = `{proId}_{seekerId}` | Both names, the job (`ServiceId`, `CityId`, `UrgencyId`, `BudgetId`), a copy of the last message, `SeekerReadAt` / `ProfessionalReadAt` | Unread = the other person wrote last after my read time. Each Inbox is one index seek (`IX_Chats_Seeker`, `IX_Chats_Professional`). Only the Seeker can start a chat. |
| `ChatMessages` | `Id` (made by the phone) | `SenderId`, `Kind` `text`/`call`, `Text` (null for a call), `At` (server time) | Clustered on (`ChatId`, `At`). A message and the chat's last-message copy are saved in one transaction. Resending the same id changes nothing. |

**Sample data** (`002-seed.sql`): 31 Skilled Workers across all trades and areas, 8 Seekers and their reviews. Every id starts `seed-` and the numbers use the unassigned `+63 900` prefix. There are no chats and no photos. It's safe to re-run, and its `DELETE` block removes the samples before go-live.

## 5. Photos (Azure Blob Storage, through `SideGigPh.Api`)

- **Where:** the storage account `sidegigphbfb9` (also the Function App's own storage; only `photos` is public), container `photos`. A photo is `https://sidegigphbfb9.blob.core.windows.net/photos/{sha256}.jpg`: readable by exact URL, not listable.
- **`POST /api/photos`** (a JPEG, PNG or iPhone HEIC, ≤ 10 MB): `PhotoImage` makes a JPEG of at most 1280px, upright, with EXIF/GPS stripped. The API hashes the stored bytes, saves `{sha}.jpg` with `owner = uid` in metadata, and returns `{id, url}` (`PhotoDto`). Another account sending a stored photo gets `409 photo-in-use` (anti-fraud rule 2).
- **`DELETE /api/photos/{id}`:** the owner only.
- **Listings** keep only photo ids (`ProfilePhotoId`, `ProfessionalWorkPhotos`); the API turns them into URLs.

## 6. API (all need `Authorization: Bearer <Firebase ID token>`)

Every error answers `{ "error": "<code>", "detail": "<why>" }`. While `ApiResults.ShowDetails` is on (MVP), `detail` holds the exception and stack trace (`500 exception`, `400 bad-body`) or what was wrong with the request (`400 invalid-{field}` echoes what was received). Turn it off before launch.

| Route | Does |
|---|---|
| `GET / PUT / DELETE users/me` | The caller's profile |
| `GET professionals?categoryId=…&areaId=…` | One trade's listings in one area, each with its stars (`Reviews`: average and count); the phone ranks them (`SearchRanker`). Kept 30 s per API instance (`ListingCache`), cleared there by a publish, removal or review |
| `GET professionals/{id}` | One listing with its stars (404 when gone) |
| `PUT professionals/me` | Publish or replace the caller's listing. `403 phone-required` with no verified phone, `409 phone-taken`, `409 photo-in-use`, `400 bad-body` (body missing or not JSON), `400 invalid-{field}` (e.g. `invalid-categoryId`; publishing also needs a `profilePhotoId`, at least 2 `workPhotoIds` and 1–3 `references` with distinct +639 numbers that aren't the worker's own) |
| `DELETE professionals/me` | Take the listing out of search (reviews stay) |
| `GET reviews?professionalId=…` / `?seekerId=…` | Newest first |
| `PUT reviews` | The caller's review (`403 no-chat` without a chat) |
| `DELETE reviews/mine` | Reviews the caller wrote |
| `GET chats?side=seeker\|professional` | The caller's Inbox |
| `GET chats/{id}`, `GET chats/{id}/messages` | Only for the two people in it |
| `POST chats/{id}/messages` | Send; the first message creates the chat (Seeker only). Returns the chat |
| `POST chats/{id}/read?side=…` | Clear the caller's unread badge |
| `DELETE chats/mine` | Every chat the caller is in |
| `POST photos`, `DELETE photos/{id}` | §5 |

**Live chat:** an open chat polls `GET chats/{id}` every 5 seconds (`ApiChatRepository.WatchInterval`) and reloads when the last message changes. Push notifications replace this later (§7).

## 7. Later (alpha feedback decides)

Push notifications for new messages (the API sends them after saving; device tokens table; a skeleton `IPushNotifications` port) · Delete account (Start over plus the Firebase account itself; Google Play requires it) · businesses / org accounts · photos in chat · block a user · review photos · a Skilled Worker's reply to a review · "available today" toggle · saved workers · report a worker or review · search logs · ID verification badge · perceptual hash for edited copies of photos · GCash · PostGIS distance search.
