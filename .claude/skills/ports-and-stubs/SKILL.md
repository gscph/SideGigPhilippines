---
name: ports-and-stubs
description: Add or change a data/infrastructure port in SideGigPh (an I*Repository / I*Store / I*Picker interface in Services/) with its stand-in adapter — a Dev/ stub that persists to a JSON file via the skeleton's JsonFile<T>, Debug-only sample data via DevSamples, DI registration in MauiProgram, the workspace (ProWorkspace / SeekerWorkspace) that pages use, and the fixed local ids used until auth. Use when a feature needs new stored data (messages, favorites, bookings, reports…), when adding a method to IInquiryRepository / IReviewRepository / IProfessionalProfileRepository / IPhotoStore, when sample data shows up where it shouldn't, or when preparing a Firestore adapter. Not for Firebase Auth (maui-firebase-auth) or UI (new-razor-component).
---

# Ports and stand-in adapters

The architecture rule (ports & adapters, no vendor lock-in) is in `CLAUDE.md`. Data ports have two adapters: the real one in `Services/Firestore/` (Cloud Firestore, the default in `MauiProgram`) and a `Dev/` stub (build with `-p:UseStubData=true`, which defines `STUB_DATA`), so a feature can be tried on one phone without a Firebase project. This skill is the recipe for both.

## The layers

| Layer | Where | Rule |
|---|---|---|
| Model (data contract) | `Models/` | `sealed record` with positional properties, ids from `ReferenceData`, times as `DateTimeOffset`. No behaviour. |
| Port | `Services/I<Thing>Repository.cs` | Async, intent-named methods (`GetForSeekerAsync`, `GetSummariesAsync`), documented ordering ("newest first"). Return what the screen needs in one call (e.g. summaries for many pros) so a real backend can answer with one query. |
| Stand-in adapter | `Services/Dev/Stub<Thing>Repository.cs` | Persists with `JsonFile<List<T>>` (skeleton, `MobileAppTheme/Services/Local/`; `dev-<things>.json`), loads once into a list, writes after each change. |
| Device adapter | Generic phone feature: `MobileAppTheme/Services/Local/` (port in `MobileAppTheme/Services/`, registered with `TryAddSingleton` in `AddMobileAppTheme`); SideGigPh-only: `SideGigPh.Maui/Services/Local/` | Real phone features (files, photo picker, dialer) behind a port, with errors logged and turned into a result, never thrown at the UI. A port any product could use (camera, location, share) belongs in the skeleton; see `mobile-app-theme`. |
| Business logic | `Services/` (`ProWorkspace`, `SeekerWorkspace`, `Services/Search/`) | Uses **ports only**. Pages call these, not the repositories. |
| Wiring | `MauiProgram.cs` | `AddSingleton<IPort, Adapter>()`; workspaces and search as singletons. |
| Firestore adapter | `SideGigPh.Maui/Services/Firestore/` | The only code that touches the Firestore SDK. One `*Doc` class per document in `FirestoreDocs.cs` matching `firebase/firestore.rules` field for field (the rules refuse any other field); map to and from the `Models/` records there. Reads that fail are logged and return empty (`FirestoreAccess.ReadAsync`); writes log and throw. A new collection or field also needs the rules (and an index for any `OrderBy`) updated, then `firebase/cloudshell-setup.sh` regenerated and pasted. Register in `AddFirestoreData()`. |

## Adding a stored feature (recipe)

1. **Model** in `Models/`. Include who it belongs to (`SeekerId`, `ProfessionalId`) so both sides can query it.
2. **Port** in `Services/`. Write the reads each screen needs; avoid "get everything then filter in the UI".
3. **Stub** in `Services/Dev/`:
   ```csharp
   public sealed class StubThingRepository(ILogger<StubThingRepository> logger) : IThingRepository
   {
       private readonly JsonFile<List<Thing>> _file = new("dev-things.json", DevJsonContext.Default.ListThing, logger);
       private List<Thing>? _things;
       private async Task<List<Thing>> AllAsync() => _things ??= await _file.ReadAsync() ?? [];
       // add: (await AllAsync()).Add(x); await _file.WriteAsync(list);
   }
   ```
   Register the list type in `Dev/DevJsonContext.cs` (`[JsonSerializable(typeof(List<Thing>))]`). The serializer is source-generated (trimming/AOT safe), so **every new persisted type needs that line**; enums are stored as strings.
4. **Samples** only behind `DevSamples.Enabled` (true in Debug, false in Release), added on read and never written to the file. Tie samples to the ids they make sense for (the pro's own listing, the sample pros), not to every id: a plumber must not show aircon reviews.
5. **DI** in `MauiProgram.cs`.
6. **Workspace**: add the state and actions to `ProWorkspace` or `SeekerWorkspace`; raise `Changed` after a save so the shell (badges, avatar) re-renders.
7. **Docs**: add the port to "Key Ports" in `CLAUDE.md`.

## Ids

A signed-in person's id is their uid: `ProWorkspace.MyId` (also the listing id) and `SeekerWorkspace.MyId`, set when the workspace loads. Use those, never a literal. `ListingMapper.OwnListingId` (`"own"`) and `SeekerWorkspace.LocalSeekerId` (`"me"`) are only the fallback when no sign-in provider is registered.

## Anti-fraud rules live in the port contract

- **One number, one listing:** `IProfessionalProfileRepository.PublishAsync` returns `PublishResult.PhoneTaken`; the stub checks it too.
- **No duplicate images:** `IPhotoStore` ids are the SHA-256 of the bytes; workspaces skip an id already in the gallery.
- A real adapter must enforce the same rule server-side; the stub's check is not a substitute.

## Gotchas

| Symptom | Cause | Fix |
|---|---|---|
| Data gone after restart | Stub kept it in memory | Persist with `JsonFile` (the published listing is still memory-only, by design, until Firestore) |
| `NotSupportedException` / empty object after reading JSON in Release | Type not in a source-generated context | Add `[JsonSerializable(typeof(List<T>))]` to `DevJsonContext` (or the store's own context) |
| A corrupt file blocks the app | Exception escaped the read | `JsonFile.ReadAsync` already logs and returns null; don't read files any other way |
| Sample rows in a Release build | Samples not behind `DevSamples.Enabled` | Gate them; Release must never show invented activity |
| UI doesn't update after an action on another page | Workspace didn't raise `Changed` | Save through the workspace's `SaveAsync`, which raises it |

## Before you finish

- `build-check` passes; registration added in `MauiProgram.cs`.
- No page injects a stub or `Local/` adapter directly (`grep -rn "Stub\|Local\." SideGigPh.Maui/Components`).
- `CLAUDE.md` "Key Ports" updated.
