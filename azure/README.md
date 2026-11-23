# azure/

SideGigPh's server side in Azure:
- the Function App **`SideGigPhApi`** (Flex Consumption, Linux, Southeast Asia) that `SideGigPh.Api` is published to: **https://api.tarbaho.com** (custom domain; its own address `https://sidegigphapi-hrb9bgerbgcgawbm.southeastasia-01.azurewebsites.net` still works)
- the **Azure SQL** database holding users, listings, reviews and chats
- the `photos` container in the storage account **`sidegigphbfb9`**

The data model is in [`docs/data-model.md`](../docs/data-model.md).

Photo URLs: `https://sidegigphbfb9.blob.core.windows.net/photos/{id}.jpg` (a new random id per upload). The API takes the account from its connection string. The database keeps only photo ids, and the API builds the URLs. **If the account changes:** change `STORAGE_ACCOUNT` in both `cloudshell-*.sh` files and `PhotoBaseUrl` in `MauiProgram`, then paste the setup script again.

The storage account is also the Function App's own storage (its deployment package and runtime files). The setup script allows public read on the account, but only the `photos` container uses it. The Function App's containers stay private.

| File | What it is |
|---|---|
| `cloudshell-setup.sh` | Paste into **Azure Cloud Shell** (https://shell.azure.com, Bash). Creates the resource group, the storage account and its `photos` container and the Function App, and puts the storage connection string into the Function App's settings. Safe to run again. It doesn't create the database (see Azure SQL below). |
| `cloudshell-github-deploy.sh` | Paste the same way, once: lets the GitHub workflow deploy `SideGigPh.Api` as a managed identity (no publish profile). See Deploy from GitHub below. |
| `cloudshell-verify.sh` | A read-only check, pasted the same way, after publishing `SideGigPh.Api`: storage, public photo URLs, settings present, and that the API answers. Prints PASS / FAIL with what to do. |

## The address: api.tarbaho.com

The domain `tarbaho.com` is registered at Cloudflare, which also holds its DNS. The API is on the subdomain `api`:
- Cloudflare DNS: `CNAME api` → `sidegigphapi-hrb9bgerbgcgawbm.southeastasia-01.azurewebsites.net`, **DNS only (grey cloud)**, and `TXT asuid.api` = the Function App's
  domain verification ID (Function App → Custom domains). With the orange proxy on, Azure can't check the domain or
  renew its certificate.
- Function App → **Custom domains**: `api.tarbaho.com` with a free **App Service Managed Certificate** (renews itself).
- Firebase → Authentication → Settings → **Authorized domains**: `api.tarbaho.com` (the operator page's sign-in).

The phone app (`ApiUrl` in `MauiProgram`), the operator page and the legal pages' links all use `api.tarbaho.com`.
To move the API to another Function App, point the `api` record at it; the app needs no new build.

## Where the connection strings go

**Only in the Function App's settings:**
- `PhotoStorage` (storage): `cloudshell-setup.sh` puts it there.
- `SqlDatabase` (Azure SQL): you add it (see Azure SQL below).
- `DocumentIntelligenceEndpoint` / `DocumentIntelligenceKey` (the AI read of a worker's ID photo, free tier): paste [`cloudshell-document-intelligence.sh`](cloudshell-document-intelligence.sh) into Cloud Shell; it also makes the private `verification` container for ID photos and proofs. Optional: without them an ID still reaches the operator, just not read by AI.
- `AdminUids` (the operators who approve verifications): you add it, the Firebase uids of the operator accounts, comma separated (Firebase console → Authentication → Users → User UID). Empty = nobody can open `ops/…` routes.
- `FirebaseServiceAccount` (sends new-message notifications): you add it, see [`firebase/README.md`](../firebase/README.md) → Notifications. Optional on a PC (leave it empty in `local.settings.json`: messages still send, nobody is notified).

To see or change them: Azure portal → Function App `SideGigPhApi` → Settings → **Environment variables**.

- **Never put them in the phone app** (not in `MauiProgram`, `appsettings`, `google-services.json` or any file in `SideGigPh.Maui`). An APK can be unzipped in a minute, and those keys give full control of every photo and every row.
- **For running `SideGigPh.Api` on your PC:** copy `SideGigPh.Api/local.settings.example.json` to `local.settings.json` (git-ignored; the host only reads that exact name). As copied:
  - `PhotoStorage` is `UseDevelopmentStorage=true`. Photos go to Azurite, the storage emulator Visual Studio starts, and the API creates the `photos` container there itself.
  - `SqlDatabase` points at SQL Server LocalDB, database `SideGigPh`. Create that database, then run the two scripts below against it.

  To use the real ones from your PC instead, paste their connection strings into `local.settings.json`, never into the `.example` file.
- The app only knows the API address (`https://api.tarbaho.com/api/`), which is not a secret. It sends the user's Firebase sign-in token; the API checks the token and holds the keys.

## First time

1. Paste `cloudshell-setup.sh` into Azure Cloud Shell. If it says a name is taken, change it at the top and paste again (`STORAGE_ACCOUNT` and `FUNCTION_APP` must be unique across all of Azure).
2. Set up Azure SQL (next section).
3. Deploy `SideGigPh.Api`: run the GitHub workflow (below), or in Visual Studio right-click **SideGigPh.Api** → **Publish** → Azure → Azure Function App → `SideGigPhApi`. **Publishing replaces every function already in that app.**
4. Paste `cloudshell-verify.sh`.

## Azure SQL

The schema and sample data are T-SQL scripts in [`SideGigPh.AzureSQL/Sql/`](../SideGigPh.AzureSQL/Sql/). `SideGigPh.AzureSQL` (EF Core) maps to them; there are no EF migrations.

1. **Create the database** (once): Azure portal → **SQL databases** → Create, in **Southeast Asia** next to the Function App. The serverless General Purpose tier (auto-pause), or the free offer, is plenty for testing. On its server, go to **Networking**: allow Azure services to access the server, and add your own IP so you can use the Query editor.
2. **Run `001-schema.sql`, then every `NNN-schema-*.sql` in number order** (`005-schema-ranking-signals.sql`, …): database → **Query editor** → sign in → paste the whole file → Run. Each is safe to run again: every step checks first and skips what exists. `SELECT * FROM dbo.SchemaChanges` lists the changes a database already has.
   Then **run `004-config.sql`** the same way (required): it fills the trades and jobs and the other answer lists (towns, price tiers…) the app shows (`GET config`). Re-run it after you edit it: it updates labels and order, adds new rows, and retires (`IsActive = 0`) any trade or job it no longer lists. Keep `ReferenceData.BuiltInConfig` in the app in step (the integration tests check). Each API instance keeps the config 12 h and phones keep theirs 12 h, so restart the Function App to apply an edit sooner; phones then pick it up within 12 h (unchanged configs cost them no download, by ETag).
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

   **`Login failed for user '<token-identified principal>'` (error 18456):** the token is fine but the database has no user for it. Either step 3 ran in `master` instead of the app's database, or the app's identity was turned off and on again after step 3 (that makes a new identity, and the old user points at the gone one). Fix: in the app's database, as the Entra admin, run

   ```sql
   DROP USER IF EXISTS [SideGigPhApi];
   CREATE USER [SideGigPhApi] FROM EXTERNAL PROVIDER;
   ALTER ROLE db_datareader ADD MEMBER [SideGigPhApi];
   ALTER ROLE db_datawriter ADD MEMBER [SideGigPhApi];
   ```

   No restart is needed. To check the mapping: `SELECT name, CONVERT(uniqueidentifier, sid) FROM sys.database_principals WHERE type = 'E';` should show `SideGigPhApi` with the app's identity's **client (application) id** (`az ad sp show --id <principalId from Identity blade> --query appId -o tsv`).
5. **Changing the schema:** the database is live, so `001-schema.sql` is frozen. Add a new guarded `NNN-schema-<what>.sql` (the `db-migration` skill: `new-migration.sh`) and change `Rows.cs` / `SideGigPhDb.cs` to match. Run the script in the Query editor as the admin **first**, then deploy the API (an API deployed first fails on the missing column).

## Deploy from GitHub

`.github/workflows/deploy-api.yml` builds and deploys `SideGigPh.Api` **only when you run it by hand**: GitHub → **Actions** → *Deploy API (Azure Functions)* → **Run workflow** (on `main`). Merging a PR deploys nothing. Before the API deploys, the database gets the schema scripts it is missing (Database migrations from GitHub below). It signs in to Azure as the **managed identity `sidegigph-github-deploy`** through OpenID Connect: GitHub proves "this is a run of `gscph/SideGigPhilippines` on `main`" and Azure trusts that. There is no password or publish profile anywhere, so it keeps working with the Function App's own managed identity on and basic auth off.

There are **two identities**, each with one job:
- the Function App's **system-assigned identity**: the running API uses it to reach Azure SQL (`Authentication=Active Directory Managed Identity`);
- the **user-assigned identity `sidegigph-github-deploy`**: GitHub signs in as it to upload code. It has *Website Contributor* on `SideGigPhApi` only.

Set it up once:

1. Paste **`cloudshell-github-deploy.sh`** into Azure Cloud Shell (you need Owner on the resource group, to hand out the role). It keeps the app's own identity on, creates the deploy identity, trusts GitHub, gives it the role, and prints three values.
2. GitHub → repo **Settings** → **Secrets and variables** → **Actions** → add them as repository secrets: `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID`. None of them is a password.
3. Delete the old secret `AZURE_FUNCTIONAPP_PUBLISH_PROFILE`. You can leave **SCM Basic Auth Publishing Credentials** off.
4. Run the workflow once by hand to check it.

- **Hosting plan:** the workflow sets `FUNCTION_SKU: flexconsumption`, matching `cloudshell-setup.sh`. If your app is on another plan, set it to `''`.
- **`AADSTS700213` / "no matching federated identity record":** Azure trusts one exact *subject*, and the run presented another. Compare it with the **subject claim** line in the run's log. GitHub writes it with the owner and repo ids (`repo:gscph@28976144/SideGigPhilippines@1449382288:ref:refs/heads/main`), not just the names. If the run wasn't from `main`, run it from `main`. Otherwise paste that line into `GITHUB_SUBJECT` in the script and paste the script again (the ids change only if the repo is renamed, moved or recreated).
- **403 / AuthorizationFailed on deploy:** the role is missing or still spreading (wait a few minutes). Paste the script again.
- **Deploy uploads, then fails reading the package:** a Flex Consumption app can read its deployment container with its own identity (portal → Function App → **Deployment settings** → *Storage authentication*). That identity then needs *Storage Blob Data Contributor* on `sidegigphbfb9`; the script adds it when it sees that setting.
- The workflow only replaces the code. App settings such as `PhotoStorage` and `SqlDatabase` stay as they are in Azure, and the only thing it changes in the database is running the new schema scripts (below).
- **The operator site** (`SideGigPh.Admin`) goes out in the same package: the workflow publishes it into `operator-site/` and the API serves it at `https://<function app host>/api/operator/` (same origin as the API: no CORS). Operators are the uids in the setting **`AdminUids`** (comma separated); a signed-in account that isn't one sees its uid on the page, to add there. Firebase's *Authorized domains* needs the Function App's host once ([`docs/architecture.md`](../docs/architecture.md) → Operator site). To run the site on your PC against the live API, paste **`cloudshell-operator-cors.sh`** (allows only `https://localhost:7265` and `http://localhost:5027`; never `*`).

## Database migrations from GitHub

The database has its own workflow, `.github/workflows/database.yml` (`db-check` → (`db-migrate`)), and like the deploy it runs **only by hand**:

- **Deploy API (by hand):** if `Sql/` or `SideGigPh.Migrate/` changed since the live API's commit (`X-Api-Version`, `.github/scripts/changed.sh`), it calls `database.yml` first and deploys only after it; otherwise it deploys without asking the database.
- **Database (by hand):** the SQL scripts only (and `004-config.sql` when ticked); it doesn't deploy the API.

The database jobs:

- **db-check** (changes nothing): a dry run of `SideGigPh.Migrate`, which lists the `NNN-schema-*.sql` scripts the live database hasn't recorded in `dbo.SchemaChanges`. Nothing pending: nothing more to do.
- **Scripts pending (a new `NNN-schema-*.sql` merged into `main`):** **db-migrate** runs exactly what `db-check` listed, straight on the live database, in number order, each script in its own transaction (all of it or none), stopping at the first failure (then the API doesn't deploy). The API deploys after it, so it never runs ahead of its tables. **Pressing Run workflow is the approval**, so read what `db-check` lists (and the PR's `Sql/` changes) before deploying. `db-check` first refuses an edited `001-schema.sql`, an unguarded step and any `DROP TABLE` (`migration-check.sh`).
- **`004-config.sql`** (the trades, jobs and answer lists) runs only when you tick **Also run 004-config.sql** on a manual run of `database.yml`. Sample data (`002`, `003`, `006`) and `001-schema.sql` never run.
- Each job opens the SQL firewall to its own runner's IP only while it runs, and removes the rule afterwards.

One-time setup:

1. **Database user for the deploy identity** (Query editor, as the Entra admin):
   ```sql
   CREATE USER [sidegigph-github-deploy] FROM EXTERNAL PROVIDER;
   ALTER ROLE db_ddladmin   ADD MEMBER [sidegigph-github-deploy];
   ALTER ROLE db_datareader ADD MEMBER [sidegigph-github-deploy];
   ALTER ROLE db_datawriter ADD MEMBER [sidegigph-github-deploy];
   ```
2. **SQL Server Contributor** for `sidegigph-github-deploy` on the SQL server (portal → the SQL server → Access control (IAM) → Add role assignment), so it can add and remove its firewall rule.
3. Optional repo variables (Settings → Secrets and variables → Actions → Variables): `SQL_SERVER` (default `sidegigph`), `SQL_DATABASE` (default `SideGigPh`), `SQL_RESOURCE_GROUP` (default `sidegigph`).

By hand, from a PC signed in with `az login`:

```bash
SQL_CONNECTION="Server=tcp:sidegigph.database.windows.net,1433;Database=SideGigPh;Authentication=Active Directory Default;Encrypt=True;" dotnet run --project SideGigPh.Migrate -- --sql SideGigPh.AzureSQL/Sql --dry-run
```

## The landing page: tarbaho.com

`SideGigPh.FrontDoor` is a static site on an **Azure Static Web App** (Free plan, deployment source *Other*); its
Early Access form calls the API, which keeps each sign-up in Azure SQL (`dbo.EarlyAccessSignups`, one row per email). Once:

1. **Static Web App:** create it (Free, deployment source *Other*). Overview → **Manage deployment token** → copy it
   straight into GitHub → Settings → Secrets → Actions as `FRONTDOOR_SWA_TOKEN` (never paste it anywhere else).
   Run *Deploy website* in Actions (or push a change to the site on `main`).
2. **Domain:** Static Web App → **Custom domains**: add `tarbaho.com` (Cloudflare: the TXT record it asks for, then
   an apex `CNAME`/flattened record to the Static Web App's address, **DNS only**) and `www.tarbaho.com` (`CNAME www`).
3. **CORS:** paste [`cloudshell-landing-cors.sh`](cloudshell-landing-cors.sh) into Cloud Shell. It allows
   `https://www.tarbaho.com`, `https://tarbaho.com` and the Static Web App's own address. Never `*`.
4. Run *Deploy API* (it runs `030-schema-early-access.sql` first), then send yourself a test sign-up and read it:
   `SELECT * FROM dbo.EarlyAccessSignups ORDER BY CreatedAt DESC` (portal Query editor). Someone who asks to be taken
   off the list: `DELETE dbo.EarlyAccessSignups WHERE Email = '…'`.
