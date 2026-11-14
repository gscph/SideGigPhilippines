---
name: ports-and-adapters
description: Add or change stored data or a data port in SideGigPh, end to end. That means the Azure SQL change (a new guarded SideGigPh.AzureSQL/Sql/NNN-schema-*.sql via the db-migration skill, plus sample rows in 002-seed.sql), the EF Core row and SideGigPhDb mapping, the store, the JSON contract (SideGigPh.Contracts + ApiJson), the Azure Function in SideGigPh.Api, the app's HTTP adapter in SideGigPh.Maui/Services/Api/, the port, DI and the workspace pages use. Use it when a feature needs new stored data (favorites, bookings, reports…), when adding a method to IChatRepository / IReviewRepository / IProfessionalProfileRepository / IUserRepository / IPhotoStore, or when changing a column. Not for Firebase sign-in (maui-firebase-auth) or UI (new-razor-component).
---

# Ports and adapters (app → API → Azure SQL)

The architecture rule (ports & adapters, no vendor lock-in) is in `CLAUDE.md`. The phone never talks to the database: pages → workspace → **port** → `Services/Api/` adapter → HTTPS + Firebase ID token → `SideGigPh.Api` function → **store** (`SideGigPh.AzureSQL`, EF Core) → Azure SQL.

## The layers

| Layer | Where | Rule |
|---|---|---|
| Schema (source of truth) | `SideGigPh.AzureSQL/Sql/001-schema.sql` (frozen: the database is live) + the numbered `NNN-schema-*.sql` changes | **Load `db-migration`.** A change is a new script, every step guarded (`IF OBJECT_ID(...) IS NULL`, `COL_LENGTH(...) IS NULL`). Named constraints. Ids are `varchar` (Firebase uids, `ReferenceData` ids, SHA-256 hex), text people type is `nvarchar`, times are `datetimeoffset(3)` UTC. An index for every read the app makes. There are no EF migrations. |
| Sample data | `SideGigPh.AzureSQL/Sql/002-seed.sql` | Every sample id starts `seed-`, and the script deletes those first, so it can run again. Phone numbers use the unassigned `+63 900` prefix. No chats, no photos. |
| Row + mapping | `SideGigPh.AzureSQL/Rows.cs`, `SideGigPhDb.cs` | One `*Row` class per table. `OnModelCreating` repeats the script's lengths, unicode, keys and index names. Strings default to `varchar` (`ConfigureConventions`), so mark `nvarchar` columns `.IsUnicode()`. |
| Store | `SideGigPh.AzureSQL/<Thing>Store.cs` | The data rules: validation, who may write what, anti-fraud. Takes the caller's uid, never trusts an id in the body. Answers with `SideGigPh.Contracts` records or an outcome enum. Multi-row writes go through `db.InTransactionAsync` (retry-safe). A unique-key race is caught with `SqlErrors.Violates(ex, Constraints.X)`. Register it in `AddSideGigPhSql`. |
| Contract | `SideGigPh.Contracts/ApiContracts.cs` + `ApiJson.cs` | `sealed record`s with camelCase JSON. Enums go over the wire as strings (`Wire.*`), errors as `ApiErrors.*`. **Every new type, and every `List<T>` of it, needs a `[JsonSerializable]` line in `ApiJson`**, because the phone needs source-generated JSON to be trim-safe. |
| Function | `SideGigPh.Api/<Thing>Functions.cs` | `AuthorizationLevel.Anonymous`. The first line is `auth.GetUidAsync` / `GetCallerAsync` (it also gives the token's verified `PhoneNumber`), and `401` without it. Read bodies with `ApiResults.ReadAsync<T>`. Map outcomes to status codes and `ApiError`. A caller's own data goes under `…/me` or `…/mine` routes. |
| App adapter | `SideGigPh.Maui/Services/Api/Api<Thing>Repository.cs` + `ApiMapping.cs` | Uses `ApiClient`. Reads go through `api.ReadAsync(…, fallback, what)`: a failure is logged and shows as empty. Writes throw after logging (`api.EnsureWritten`), and the pages show "didn't send / didn't publish". Map contracts ↔ `Models/` only in `ApiMapping`. Register it in `AddSideGigPhApi`. |
| Port + model | `SideGigPh.Maui/Services/I<Thing>Repository.cs`, `Models/` | Async, intent-named methods with documented ordering ("newest first"). Return what a screen needs in one call, so the API answers with one query. Models never see contract or EF types. |
| Business logic | `Services/` (`ProWorkspace`, `SeekerWorkspace`, `Services/Search/`) | **Ports only.** Pages call these, not repositories. Raise `Changed` after a save. |

Device adapters (files, photo picker, dialer) are another kind of adapter. A generic one lives in `MobileAppTheme/Services/Local/`, a SideGigPh-only one in `SideGigPh.Maui/Services/Local/`; see `mobile-app-theme`.

## Adding a stored feature (recipe)

1. Make a new schema change (`db-migration` skill: `new-migration.sh <what>`) with the table, keys and indexes, each step guarded; never edit `001-schema.sql`. Add sample rows to `002-seed.sql` if the UI needs something to show.
2. Add the `*Row` and its `OnModelCreating` block (the same lengths and names as the script), and a `DbSet`.
3. Add a store method holding the rules, and register it in `AddSideGigPhSql`.
4. Add the contract records plus their `[JsonSerializable]` lines.
5. Add a function with the auth check first.
6. Add the port method, model, adapter method and mapping. Register a new adapter in `AddSideGigPhApi`.
7. Add the state and actions to the workspace.
8. Update `docs/architecture.md` (Key Ports), `CLAUDE.md` (its Architecture map line, and the schema notes if a table changed) and `docs/data-model.md`.
9. Run the new SQL against the database (portal Query editor) **before** deploying the API that needs it.

## Ids

A signed-in person's id is their Firebase uid: `ProWorkspace.MyId` (also the listing id) and `SeekerWorkspace.MyId`. The server always takes the uid from the token. A chat is `{professionalId}_{seekerId}` (`Chat.IdFor` / `ChatStore.IdFor`). `ListingMapper.OwnListingId` (`"own"`) and `SeekerWorkspace.LocalSeekerId` (`"me"`) are only the fallback when no sign-in provider is registered.

## Anti-fraud rules are enforced in the database and the store

- **One number, one listing:** `UQ_Professionals_MobileNumber`. The listing's number is the token's `phone_number`, so publishing needs a verified phone (`403 phone-required`). A clash gives `409 phone-taken` → `PublishResult.PhoneTaken`.
- **No duplicate images:** the photo id is the SHA-256 of the stored JPEG. `PK_ProfessionalWorkPhotos` (global) and `UX_Professionals_ProfilePhoto` stop two listings sharing a photo (`409 photo-in-use`). The upload API also refuses a photo another account stored.

## Gotchas

| Symptom | Cause | Fix |
|---|---|---|
| `Invalid column name` / `Invalid object name` from the API | The SQL wasn't run on the database, or the script and `SideGigPhDb` disagree | Run the script; make the mapping match it |
| `NotSupportedException` / empty object reading JSON on the phone | The type isn't in `ApiJson` | Add `[JsonSerializable(typeof(T))]` and `List<T>` |
| A screen is empty but the API works in a browser | The adapter's read failed and fell back to empty | Check Logcat for `API: couldn't read …` (status code, JSON) |
| `401` on every call | No ID token (signed out), or `FirebaseProjectId` is wrong on the Function App | Sign in; fix the setting |
| A just-linked phone still gets `403 phone-required` | Stale ID token without `phone_number` | `LinkPhoneAsync` refreshes it; any new linking path must too |
| The UI doesn't update after an action on another page | The workspace didn't raise `Changed` | Save through the workspace |

## Before you finish

- The API project builds (copy it to the scratchpad and run `dotnet build`) and `build-check` passes for the app.
- No page injects an adapter (`grep -rn "Services.Api" SideGigPh.Maui/Components` finds nothing).
- `001-schema.sql` + the `NNN-schema-*.sql` changes and `SideGigPhDb` agree; `migration-check.sh` passes; `CLAUDE.md` is updated.
