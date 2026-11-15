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
| `SideGigPh.AzureSQL` | `Sql/001-schema.sql` (frozen) and the later `Sql/NNN-schema-*.sql` changes, `Sql/004-config.sql` (the trades, jobs and other answer lists), `Sql/002-seed.sql`, the `*Row` classes, `SideGigPhDb` (mapping) and the stores (`UserStore`, `ProfessionalStore`, `ReviewStore`, `ChatStore`, `PushDeviceStore`, `CatalogStore`), which hold the data rules. |
| `SideGigPh.Api` | One function class per area (`UserFunctions`, `ProfessionalFunctions`, `ReviewFunctions`, `ChatFunctions`, `PhotoFunctions`, `ConfigFunctions`). Each checks the token first, then calls a store. `ChatNotifier` + `IPushSender` send new-message notifications. |
| `SideGigPh.Maui/Services/Api/` | `ApiClient`, `Api*Repository` adapters, `ApiPhotoStore`, `ApiMapping` (contracts ↔ `Models/`). Registered by `AddSideGigPhApi()`. |

- **Reads that fail** (offline, server error) are logged and come back empty; the screens already have empty states. **Writes** log and throw, and the pages say "didn't send / didn't publish".
- **The server always takes the uid from the token**, never from the body. A person's own data sits under `…/me` / `…/mine` routes.
- **Swapping the database again** (PostgreSQL, PostGIS…) touches only `SideGigPh.AzureSQL`: the API's contracts and the app don't change.

## 4. Tables

All created by `001-schema.sql` plus the later schema changes (`NNN-schema-*.sql`, recorded in `dbo.SchemaChanges`; the first, `005-schema-ranking-signals.sql`, added `Chats.ProfessionalRepliedAt` and `Users.LastActiveAt`). Ids are `varchar`: Firebase uids, catalog ids (`aircon`, `aircon-cleaning`, rows of `Categories` / `Services`), `ReferenceData` ids (`rizal`, `taytay`, `500-1000`…), and SHA-256 hex for photos. All these ids are stable. Listings, chats and reviews keep category/service ids as plain text with no foreign key, so a retired trade's old rows still read. Times are `datetimeoffset(3)` UTC.

| Table | Key | What | Notes |
|---|---|---|---|
| `Users` | `Uid` | The person: name, verified phone, email, `language` (`english`/`taglish`), `role` (`seeker`/`professional`), private `IncomeGoalId`, `ProfilePhotoId` (the account's one profile photo, unique: `UX_Users_ProfilePhoto`; the phone is indexed by `IX_Users_PhoneNumber`, `009-schema-users-phone-index.sql`, for the publish check of anti-fraud rule 1), `LastActiveAt` (a worker last read their Inbox, stamped at most hourly; search ranking) | Only that person reads or writes it (`users/me`). The interview answers stay on the phone (`IOnboardingStore`). |
| `Professionals` | `Id` = uid | The published listing: name, `MobileNumber`, one `CategoryId`, one `AreaId`, `ExperienceId`, `RateTierId`, `ProfilePhotoId`, `PublishedAt` (first went live), `UpdatedAt` | **`UQ_Professionals_MobileNumber`** (anti-fraud rule 1); the number is the token's verified `phone_number`. `IX_Professionals_Category_Area` serves search. The row exists only while published. |
| `ProfessionalServices` | (`ProfessionalId`, `ServiceId`) | The jobs they do | Cascade delete with the listing |
| `ProfessionalCities` | (`ProfessionalId`, `CityId`) | Towns they go to, inside the area | Cascade delete |
| `ProfessionalWorkPhotos` | `PhotoId` | 2–6 work photos (the check allows 12), `Position` 0 = newest | **The PK is global** (anti-fraud rule 2), as is `UX_Professionals_ProfilePhoto`. URLs are built from the id. |
| `ProfessionalReferences` | (`ProfessionalId`, `Position`) | 1–3 people who vouch for the worker: `Name`, `MobileNumber` (+639…) | **Private**: only the count is in `ListingDto` (`ReferenceCount`, one `GROUP BY` like the stars), never names or numbers. Replaced on every publish; cascades with the listing. |
| `Reviews` | (`ProfessionalId`, `SeekerId`) | One per Seeker per worker: `ReviewerName`, `Rating` 1–5, `Comment` ≤ 1000, `ServiceId` | Only after a chat between the two exists. **No FK to `Professionals`**, so a worker who starts over can't wipe their reviews. Averages come from one `GROUP BY` and travel on each listing (`ListingDto.Reviews`). |
| `Chats` | `Id` = `{proId}_{seekerId}` | Both names, the job (`ServiceId`, `CityId`, `UrgencyId`, `BudgetId`), a copy of the last message, `SeekerReadAt` / `ProfessionalReadAt`, `ProfessionalRepliedAt` (the worker's first reply; search ranking) | Unread = the other person wrote last after my read time. Each Inbox is one index seek (`IX_Chats_Seeker`, `IX_Chats_Professional`). Only the Seeker can start a chat. |
| `ChatMessages` | `Id` (made by the phone) | `SenderId`, `Kind` `text`/`call`, `Text` (null for a call), `At` (server time) | Clustered on (`ChatId`, `At`). A message and the chat's last-message copy are saved in one transaction. Resending the same id changes nothing. |
| `PushDevices` | `Token` | One row per phone that gets the person's notifications: `Uid`, `Platform` (`android`/`ios`), `UpdatedAt` | A token belongs to whoever registered it last (a phone that changes account moves). `IX_PushDevices_Uid` serves each send. Tokens Google calls dead are deleted after a send; Start over (`DELETE users/me`) deletes the person's. |
| `Categories` | `Id` | The trades: `LabelEnglish` / `LabelTaglish`, `Icon` (an emoji, only for app versions before the app drew its own pictures by id: `AppImages`), hints, `SortOrder`, `IsActive` | Filled by **`004-config.sql`** (required data; re-run it after editing: it upserts, and retires rows it no longer lists). `IsActive = 0` = retired (e.g. `home_massage`): not served, id kept. Served by `GET config` (kept 12 h per API instance, ETag-versioned). |
| `Services` | `Id` | The jobs of a trade (`CategoryId` FK): labels, `Icon`, `SortOrder` (most-requested first, `…-other` "Others / Wala sa nabanggit" last), `IsActive`, and the price guide `PriceMin` / `PriceAverage` / `PriceHigh` (pesos, labor only; `008-schema-service-prices.sql`) | `IX_Services_Category`, `CK_Services_Prices` (all null, or min ≤ average ≤ high). Ids are unique across trades. `PriceMin` is never under the `minimum-wage` option. |
| `OtherJobNotes` | `Id` (identity) | What someone typed after picking "Others": `Uid`, `Side` (`seeker`/`professional`), `CategoryId` (a trade, or `other` for a kind of work we don't offer, typed under the "Others" tile after the trades), `Text` ≤ 100, `CreatedAt` | **Analytics only**: never becomes a job, never shown. `DELETE users/me` sets `Uid` to null and keeps the note. Query e.g. `SELECT CategoryId, Text, COUNT(*) FROM dbo.OtherJobNotes GROUP BY CategoryId, Text ORDER BY 3 DESC`. |
| `ErrorReports` | `Id` (identity) | Problems the app had on a phone (`010-schema-error-reports.sql`): `InstallId` (random per install, **not** the account), `AppVersion` (store version and build), `CodeVersion` (git commit, `-dirty` for a local build with changes), `Channel` (`dev`/`beta`/`prod`), `Platform` (OS and version), `Device` (maker and model), `Kind` (`error`/`crash`/`slow`), `Source` (logger category), `Message`, `Detail` (stack trace), `Route` (screen), `Trail` (screens and steps before it), `Count` (repeats on the phone), `FirstAt`, `LastAt`, `ReceivedAt` | **Diagnostics only, no personal data**: no uid; texts scrubbed on the phone and again on the server (`ErrorReportRules.Scrub`: tokens, numbers, emails, uids, photo hashes, query strings). Deleted after 90 days (`ErrorReportStore.KeepFor`, on each save). Query e.g. `SELECT TOP 50 Kind, Source, Message, Route, SUM(Count) n, MAX(LastAt) last FROM dbo.ErrorReports GROUP BY Kind, Source, Message, Route ORDER BY n DESC`. |
| `Reports` | `Id` (identity) | A user reporting another (`011-schema-safety.sql`): `ReporterUid` (null once the reporter deleted their account), `TargetUid`, `Kind` (`listing`/`review`/`chat`/`user`), `SubjectId` (the chat id), `Reason` (`scam`/`harassment`/`fake`/`inappropriate`/`employment`/`other`), `Note` ≤ 500, `Status` (`open`/`actioned`/`dismissed`), `CreatedAt`, `ResolvedAt` | Read by the operator, answered within 5 business days (`IX_Reports_Status`; queries in [`docs/legal/README.md`](legal/README.md)). Reports about someone stay when they delete their account, until resolved. |
| `Blocks` | (`BlockerUid`, `BlockedUid`) | Who blocked whom, `CreatedAt` | **Either direction hides the two from each other**: search (filtered after the shared cache), `GET professionals/{id}` (404), the Inbox, and new messages (`403 blocked`). `IX_Blocks_Blocked` finds who blocked the caller. Deleted with either account. |
| `FeedbackPosts` | `Id` (identity) | A feature someone suggested (`012-schema-feedback.sql`): `AuthorUid` (null once the writer deleted their account), `Side` (`seeker`/`professional`), `Title` ≤ 100, `Details` ≤ 600, `Votes`, `Status` (`open`/`planned`/`done`/`declined`/`hidden`), `CreatedAt` | Shown to every signed-in user **without the writer**. `Votes` is the count of its `FeedbackVotes` rows, recounted on every vote. The operator sets `Status`; `declined` and `hidden` leave the board. `IX_FeedbackPosts_Top` (`Status`, `Votes` desc, `CreatedAt` desc) |
| `FeedbackVotes` | (`PostId`, `VoterUid`) | One vote per person per suggestion, `CreatedAt` | Cascade delete with the suggestion; removed when the voter deletes their account. `IX_FeedbackVotes_Voter` |
| `ConfigOptions` | (`ListId`, `Id`) | The other answer lists of the app config: `ListId` `towns` / `price-tiers` (old one-scale tiers, read only for app versions before price guides) / `experience` / `urgencies` / `income-goals` / `minimum-wage` (one row, `ncr`, ₱755 a day), labels, `Icon`, `Amount` (pesos for price tiers, goals and the wage, else 0), `SortOrder`, `IsActive` | Filled by `004-config.sql` like `Categories`; served in `GET config`. The ids the code names (`10-plus`, the urgency ids, `rizal` towns) must stay. |

**Sample data** (`002-seed.sql`): 31 Skilled Workers across all trades and areas, 8 Seekers and their reviews. Every id starts `seed-` and the numbers use the unassigned `+63 900` prefix. There are no chats and no photos. It's safe to re-run, and its `DELETE` block removes the samples before go-live.

**Search ranking test data** (`006-seed-search-ranking.sql`): 45 sample workers tagged `seed-s2-`, with reviews, chats (answered, ignored, call-only, fresh) and activity/tenure dates relative to now, one trade per ranking signal. How to use it: [search-ranking-test-plan.md](search-ranking-test-plan.md). Set `@RemoveOnly = 1` to remove it.

## 5. Photos (Azure Blob Storage, through `SideGigPh.Api`)

- **Where:** the storage account `sidegigphbfb9` (also the Function App's own storage; only `photos` is public), container `photos`. A photo is `https://sidegigphbfb9.blob.core.windows.net/photos/{sha256}.jpg`: readable by exact URL, not listable.
- **`POST /api/photos`** (a JPEG, PNG or iPhone HEIC, ≤ 10 MB): `PhotoImage` makes a JPEG of at most 1280px, upright, with EXIF/GPS stripped. The API hashes the stored bytes, saves `{sha}.jpg` with `owner = uid` in metadata, and returns `{id, url}` (`PhotoDto`). Another account sending a stored photo gets `409 photo-in-use` (anti-fraud rule 2).
- **`DELETE /api/photos/{id}`:** the owner only.
- **Listings** keep only photo ids (`ProfilePhotoId`, `ProfessionalWorkPhotos`); the API turns them into URLs.

## 6. API (all but `GET config` and `GET legal/{page}` need `Authorization: Bearer <Firebase ID token>`)

Every answer carries `X-Api-Version` (the API build, e.g. `0.1.0-rc.57+48466c9`) and the app sends `X-App-Version` (`0.1.0-rc.12 (beta; 48466c9)`) on every call; the server logs each crash and refusal with it ([architecture → Versioning](architecture.md#versioning)).

Every error answers `{ "error": "<code>", "detail": "<why>" }`. While `ApiResults.ShowDetails` is on (MVP), `detail` holds the exception and stack trace (`500 exception`, `400 bad-body`) or what was wrong with the request (`400 invalid-{field}` echoes what was received). Turn it off before launch.

| Route | Does |
|---|---|
| `GET / PUT / DELETE users/me` | The caller's profile (DELETE also forgets their phones; Start over) |
| `DELETE users/me/account` | **Delete account**: in one transaction the listing, chats (both sides), reviews written and received, blocks, phones and profile; reports they made lose their uid; then their photos and the Firebase account (`IAccountDeleter`, the `FirebaseServiceAccount` setting). `204`, or `202` when the server can't delete the Firebase account and the app must |
| `POST reports` | Report a person (`ReportRequest`: `targetUid`, `kind`, `reason`, `subjectId`, `note`, `alsoBlock`). 10 an hour |
| `PUT blocks/{uid}` | Block a person (see `Blocks`) |
| `GET feedback?skip=0&take=20` | One page of the feedback board (`FeedbackPageDto`: `posts`, `hasMore`), open and planned suggestions most voted first, finished ones last; each `FeedbackPostDto` says `votedByMe` and `mine`, never who wrote it |
| `GET feedback/similar?q=…` | Up to 5 suggestions that probably ask for the same thing as `q` (`FeedbackMatch`) |
| `POST feedback` | A new suggestion (`NewFeedbackRequest`: `title` 6–100, `details`, `side`); the caller's vote is its first. `400 invalid-title`. 10 an hour |
| `PUT` / `DELETE feedback/{id}/vote` | Add or take back the caller's vote; answers `FeedbackVoteDto` (`votes`, `votedByMe`). `404` when it isn't on the board. 120 an hour |
| `DELETE feedback/{id}` | The caller's own suggestion, only while nobody else voted (`404` otherwise) |
| `POST feedback/{id}/report` | Report a suggestion: a row in `Reports` about its writer, `SubjectId` `feedback:{id}` |
| `GET legal/{page}` | Public HTML: `privacy`, `terms`, `data` (from `docs/legal/*.md`, `LegalFunctions`) |
| `PUT users/me/devices` | This phone gets the caller's notifications (`SaveDeviceRequest`: `token`, `platform`). `400` for an empty token or unknown platform |
| `GET search/professionals?categoryId=…&areaId=…&serviceId=…&cityId=…&budgetId=…&skip=0&take=4` | One page of a trade's workers ranked for the Seeker on the server (`SearchRanker`, [search-ranking.md](search-ranking.md): exact job only; their town first, then other served towns; a 0–100 score from reviews, responsiveness, activity, budget and experience), as a `SearchPageDto` (hits in ranked order + `InTownCount`, `OtherTownsCount`, `ServiceFallback` (always false now)). Only `categoryId` is required; `take` defaults to 4, at most 50; `400 invalid-skip` / `invalid-take` |
| `GET professionals?categoryId=…&areaId=…` | For app versions before paging: one trade's listings in one area, each with its stars (`Reviews`: average and count), unranked. Both search routes share the 30 s per-instance `ListingCache`, cleared there by a publish, removal or review |
| `GET professionals/{id}` | One listing with its stars (404 when gone) |
| `PUT professionals/me` | Publish or replace the caller's listing. `403 phone-required` with no verified phone, `409 phone-taken` (only a race: another account still showing the caller's verified number loses it, its listing comes down and its profile's number is cleared; `409 phone-used-by-seeker` is retired), `409 photo-in-use`, `400 bad-body` (body missing or not JSON), `400 invalid-{field}` (e.g. `invalid-categoryId`; publishing also needs a `profilePhotoId`, at least 2 `workPhotoIds` and 1–3 `references` with distinct +639 numbers that aren't the worker's own) |
| `DELETE professionals/me` | Take the listing out of search (reviews stay) |
| `GET reviews?professionalId=…` / `?seekerId=…` | Newest first |
| `PUT reviews` | The caller's review (`403 no-chat` without a chat, `403 review-too-soon` in the first 24 hours of the chat unless changing an earlier review) |
| `DELETE reviews/mine` | Reviews the caller wrote |
| `GET chats?side=seeker\|professional` | The caller's Inbox |
| `GET chats/{id}`, `GET chats/{id}/messages?take=20&before={messageId}` | Only for the two people in it. Messages oldest first: the newest `take` (at most 100) before message `before`, or the latest without it; without `take`, every message (app versions before paging). `400 invalid-take` |
| `POST chats/{id}/messages` | Send; the first message creates the chat (Seeker only). Notifies the other person (§6.1). Returns the chat |
| `POST chats/{id}/read?side=…` | Clear the caller's unread badge |
| `DELETE chats/mine` | Every chat the caller is in |
| `POST photos`, `DELETE photos/{id}` | §5 |
| `GET config` | **Public** (no token): the app config (`ConfigDto`): active trades with their active jobs, towns, price tiers, experience, urgencies, income goals, in order. Answers an `ETag` (hash of the content) and `Cache-Control: public, max-age=43200`; with a matching `If-None-Match` it answers `304` and no body. The phone loads it once per launch, at most every 12 h, and keeps it for the next launch, even offline |
| `POST other-jobs` | What the caller typed under "Others" (`OtherJobRequest`: `categoryId`, `side`, `text`). `400` for empty text, a bad id or side |
| `POST error-reports` | Up to 20 error reports from one install (`ErrorReportBatch`: `installId`, `appVersion`, `codeVersion`, `channel`, `platform`, `device`, `reports`). Signed in only (the uid counts the limit, 12 batches an hour, and is never stored). `400` for a bad install id, channel, kind or count of reports, or a body over 256 KB; the app drops a refused batch |

**Live chat:** an open chat polls `GET chats/{id}` every 5 seconds (`ApiChatRepository.WatchInterval`) and reloads when the last message changes. A message that arrives while the app is open but the chat isn't refreshes the Inbox through its notification (§6.1).

### 6.1 New-message notifications (Firebase Cloud Messaging)

Not SignalR: SignalR only reaches an app that is open, and the open app already polls. A notification reaches a closed phone.

- **Server:** after `ChatStore.SendAsync` saves a **new** message (a retried message id sends nothing), `ChatNotifier` reads the receiver's phones, language and unread-chat count (`PushDeviceStore.TargetAsync`) and sends through `IPushSender` (`FcmPushSender`, Firebase Admin SDK, setting **`FirebaseServiceAccount`**: the Firebase project's service-account JSON; without it `NoPushSender` logs and nobody is told). A failed notification is logged and never fails the message.
- **Customer → Skilled Worker = loud:** Android channel `customers` (high importance), iOS alert + sound. Title "New customer: Liza" / "Bagong customer: Liza" on the first message, then just the name; body = the message (≤ 120 chars, one line) or "Called you" / "Tumawag sa'yo".
- **Skilled Worker → Seeker = quiet:** Android channel `replies` (low importance: no sound, no pop-up, only the tray line that puts the dot on the icon); iOS badge only, no alert.
- **Every notification** has the chat id as its tag (Android) and `apns-collapse-id` (iOS), so one chat = one notification; the badge number = the receiver's unread chats; data `chatId`, `professionalId`, `side` (`PushData`); and `is_silent_in_foreground` so an open app shows nothing and refreshes its Inbox instead.
- **App:** `ChatAlerts` (`Services/`) registers the phone (`IUserRepository.SaveDeviceAsync`), asks to allow notifications only with a reason (a Skilled Worker with a live listing when Pro home opens; a Seeker right after sending a message; never at first launch), opens the chat on a tap (`ChatAlerts.PathFor`), and on every Inbox change calls `IPushNotifications.ShowOnly(unread chat ids)`: read chats leave the tray and the icon badge follows.


## 7. Later (alpha feedback decides)

businesses / org accounts · photos in chat · review photos · a Skilled Worker's reply to a review · "available today" toggle · saved workers · unblocking · search logs · ID verification badge · perceptual hash for edited copies of photos · GCash · PostGIS distance search.
