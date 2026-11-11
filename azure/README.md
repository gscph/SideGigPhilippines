# azure/

Photo storage for SideGigPh: the `photos` container in the existing storage account **`realtimeordertracker`**, plus the Function App that `SideGigPh.Api` is published to. The data model is [`docs/data-model.md`](../docs/data-model.md) §5.

Photo URLs: `https://realtimeordertracker.blob.core.windows.net/photos/{sha256}.jpg`. The API takes the account from its connection string, so it needs no separate URL setting. The URL also appears in `firebase/firestore.rules` (`photoUrl`), which accepts only that address for a photo. **If the account changes:** change `STORAGE_ACCOUNT` in both `cloudshell-*.sh` files and `photoUrl` in the rules, then paste the setup scripts again.

The account is shared with another app. The setup script allows public read on the account, but only the `photos` container uses it. The other app's containers stay private. The Function App also keeps its own deployment files in this account.

| File | What it is |
|---|---|
| `cloudshell-setup.sh` | Paste into **Azure Cloud Shell** (https://shell.azure.com, Bash). Creates the resource group, the storage account and its `photos` container, the Function App, and puts the storage connection string into the Function App's settings. Safe to run again. |
| `cloudshell-verify.sh` | Read-only check, pasted the same way, after publishing `SideGigPh.Api`: storage, public photo URLs, settings present, and that the API answers. Prints PASS / FAIL with what to do. |

## Where the connection string goes

**Only in the Function App's settings, as `PhotoStorage`.** `cloudshell-setup.sh` puts it there for you. To see or change it by hand: Azure portal → Function App `realtimeordertracking` → Settings → **Environment variables** → `PhotoStorage`.

- **Never in the phone app** (not in `MauiProgram`, `appsettings`, `google-services.json` or any file in `SideGigPh.Maui`). An APK can be unzipped in a minute, and that key gives full control of every photo.
- **For running `SideGigPh.Api` on your PC:** copy `SideGigPh.Api/local.settings.example.json` to `local.settings.json` (git-ignored; the host only reads that exact name). As copied, `PhotoStorage` is `UseDevelopmentStorage=true`: photos go to Azurite, the storage emulator Visual Studio starts, and the API creates the `photos` container there itself. To hit the real account from your PC instead, paste its connection string into `local.settings.json`, never into the `.example` file.
- The app only knows the API address (`https://fredhopper-bdhed2e6dtegb9gz.uksouth-01.azurewebsites.net/api/`), which is not a secret. It sends the user's Firebase sign-in token; the API checks it and owns the storage key.

## First time

1. Paste `cloudshell-setup.sh` into Azure Cloud Shell. If it says a name is taken, change it at the top and paste again (`STORAGE_ACCOUNT` and `FUNCTION_APP` must be unique across all of Azure).
2. Visual Studio: right-click **SideGigPh.Api** → **Publish** → Azure → Azure Function App → `realtimeordertracking`. **Publishing replaces every function already in that app**: `cloudshell-setup.sh` lists any other functions it finds there and warns you first. If it lists some, use a separate Function App (change `FUNCTION_APP` in both scripts).
3. Paste `cloudshell-verify.sh`.
