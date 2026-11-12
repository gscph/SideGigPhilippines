# azure/

SideGigPh's server side in Azure:
- the Function App **`SideGigPhApi`** (Flex Consumption, Linux, Southeast Asia) that `SideGigPh.Api` is published to: https://sidegigphapi-hrb9bgerbgcgawbm.southeastasia-01.azurewebsites.net
- the **Azure SQL** database holding users, listings, reviews and chats
- the `photos` container in the storage account **`sidegigphbfb9`**

The data model is in [`docs/data-model.md`](../docs/data-model.md).

Photo URLs: `https://sidegigphbfb9.blob.core.windows.net/photos/{sha256}.jpg`. The API takes the account from its connection string. The database keeps only photo ids, and the API builds the URLs. **If the account changes:** change `STORAGE_ACCOUNT` in both `cloudshell-*.sh` files and `PhotoBaseUrl` in `MauiProgram`, then paste the setup script again.

The storage account is also the Function App's own storage (its deployment package and runtime files). The setup script allows public read on the account, but only the `photos` container uses it. The Function App's containers stay private.

| File | What it is |
|---|---|
| `cloudshell-setup.sh` | Paste into **Azure Cloud Shell** (https://shell.azure.com, Bash). Creates the resource group, the storage account and its `photos` container and the Function App, and puts the storage connection string into the Function App's settings. Safe to run again. It doesn't create the database (see Azure SQL below). |
| `cloudshell-verify.sh` | A read-only check, pasted the same way, after publishing `SideGigPh.Api`: storage, public photo URLs, settings present, and that the API answers. Prints PASS / FAIL with what to do. |

## Where the connection strings go

**Only in the Function App's settings:**
- `PhotoStorage` (storage): `cloudshell-setup.sh` puts it there.
- `SqlDatabase` (Azure SQL): you add it (see Azure SQL below).

To see or change them: Azure portal → Function App `SideGigPhApi` → Settings → **Environment variables**.

- **Never put them in the phone app** (not in `MauiProgram`, `appsettings`, `google-services.json` or any file in `SideGigPh.Maui`). An APK can be unzipped in a minute, and those keys give full control of every photo and every row.
- **For running `SideGigPh.Api` on your PC:** copy `SideGigPh.Api/local.settings.example.json` to `local.settings.json` (git-ignored; the host only reads that exact name). As copied:
  - `PhotoStorage` is `UseDevelopmentStorage=true`. Photos go to Azurite, the storage emulator Visual Studio starts, and the API creates the `photos` container there itself.
  - `SqlDatabase` points at SQL Server LocalDB, database `SideGigPh`. Create that database, then run the two scripts below against it.

  To use the real ones from your PC instead, paste their connection strings into `local.settings.json`, never into the `.example` file.
- The app only knows the API address (`https://sidegigphapi-hrb9bgerbgcgawbm.southeastasia-01.azurewebsites.net/api/`), which is not a secret. It sends the user's Firebase sign-in token; the API checks the token and holds the keys.

## First time

1. Paste `cloudshell-setup.sh` into Azure Cloud Shell. If it says a name is taken, change it at the top and paste again (`STORAGE_ACCOUNT` and `FUNCTION_APP` must be unique across all of Azure).
2. Set up Azure SQL (next section).
3. Deploy `SideGigPh.Api`: run the GitHub workflow (below), or in Visual Studio right-click **SideGigPh.Api** → **Publish** → Azure → Azure Function App → `SideGigPhApi`. **Publishing replaces every function already in that app.**
4. Paste `cloudshell-verify.sh`.

## Azure SQL

The schema and sample data are T-SQL scripts in [`SideGigPh.AzureSQL/Sql/`](../SideGigPh.AzureSQL/Sql/). `SideGigPh.AzureSQL` (EF Core) maps to them; there are no EF migrations.

1. **Create the database** (once): Azure portal → **SQL databases** → Create, in **Southeast Asia** next to the Function App. The serverless General Purpose tier (auto-pause), or the free offer, is plenty for testing. On its server, go to **Networking**: allow Azure services to access the server, and add your own IP so you can use the Query editor.
2. **Run `001-schema.sql`:** database → **Query editor** → sign in → paste the whole file → Run. It's safe to run again; it only creates what's missing.
3. **Run `002-seed.sql`** the same way. It adds sample Skilled Workers and reviews, with no chats and no photos, and is safe to run again. Before go-live, run only its `DELETE` block to take the samples out.
4. **Give the API the connection string:** database → **Connection strings** → ADO.NET (SQL authentication). Put the password in. Then go to Function App → Environment variables, add `SqlDatabase` with that value, and Apply. The Function App restarts.

   **Better: no password anywhere.**
   1. Turn on the Function App's **system-assigned managed identity**.
   2. Make yourself the server's **Microsoft Entra admin**.
   3. Run this in the database:

      ```sql
      CREATE USER [SideGigPhApi] FROM EXTERNAL PROVIDER;
      ALTER ROLE db_datareader ADD MEMBER [SideGigPhApi];
      ALTER ROLE db_datawriter ADD MEMBER [SideGigPhApi];
      ```

   4. Set `SqlDatabase` to `Server=tcp:<server>.database.windows.net,1433;Database=<db>;Authentication=Active Directory Managed Identity;Encrypt=True;`.
5. **Changing the schema:** edit `001-schema.sql` **and** `SideGigPhDb.cs` together, run the new part against the database, then deploy the API.

## Automatic deploy from GitHub

`.github/workflows/deploy-api.yml` builds and deploys `SideGigPh.Api` when you run it by hand: GitHub → **Actions** → *Deploy API (Azure Functions)* → **Run workflow**. It signs in to Azure with the Function App's **publish profile**, kept as a GitHub secret. Set it up once:

1. **Allow publish profiles.** Azure portal → Function App `SideGigPhApi` → Settings → **Configuration** → **General settings** → turn **SCM Basic Auth Publishing Credentials** to **On** → Save. (New apps have it off, and then the download button below is greyed out.)
2. **Download it.** Function App → **Overview** → **Download publish profile** (older portal: *Get publish profile*). You get a `SideGigPhApi.PublishSettings` file: an XML file with a deploy password inside. Treat it like a password: don't commit it, and delete it from Downloads after step 3.
3. **Store it in GitHub.** Repo → **Settings** → **Secrets and variables** → **Actions** → **New repository secret**. Name it `AZURE_FUNCTIONAPP_PUBLISH_PROFILE`; the value is the **whole** contents of the file (open it in Notepad, select all, paste).
4. Run the workflow once by hand to check it.

- **Hosting plan:** the workflow sets `FUNCTION_SKU: flexconsumption`, matching `cloudshell-setup.sh`. If your app is on another plan (portal → Overview → *App Service plan* / *Plan type*), set it to `''`.
- **If the deploy fails with 401 / Unauthorized:** the profile is stale (someone pressed *Reset publish profile*, or basic auth was turned off). Download it again and replace the secret.
- The workflow only replaces the code. App settings such as `PhotoStorage` and `SqlDatabase` stay as they are in Azure, and it never touches the database: run SQL changes yourself first.
