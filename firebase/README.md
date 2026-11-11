# firebase/

Server-side setup for SideGigPh's Firestore (MVP). The data model is [`docs/data-model.md`](../docs/data-model.md). Photos are **not** in Firebase: they're in Azure Blob Storage, behind `SideGigPh.Api` (see [`azure/`](../azure/README.md)).

| File | What it is |
|---|---|
| `firestore.rules` | Who may read/write each collection, and which fields and values are allowed. Firestore has no server in front of it, so the rules act as the API and the schema. |
| `firestore.indexes.json` | The composite indexes the Inbox (chats) and review lists need. Search uses only equality filters, which need no composite index. |
| `firebase.json` | Ties the above together; emulator ports. |
| `setup.ps1` | Creates the database and deploys everything, from your PC (needs the Firebase CLI). |
| `cloudshell-setup.sh` | The same, plus the Philippines-only SMS policy and a `_meta/schema` doc, for **Google Cloud Shell**: paste the whole file, nothing to install. It embeds a copy of `firestore.rules`, so regenerate it when the rules change. |
| `cloudshell-verify.sh` | Read-only check, also pasted into Cloud Shell: APIs, database location, live rules, indexes (built or still building), SMS policy, and a few pretend requests run against the live rules. Prints PASS / WAIT / FAIL with what to do. |

## First time

1. Firebase console: put the project (the one whose `google-services.json` the app uses) on the **Blaze** plan (needed for SMS sign-in).
2. Install the CLI and log in:

```powershell
npm install -g firebase-tools
```

```powershell
firebase login
```

3. Create the database and deploy:

```powershell
./firebase/setup.ps1 -ProjectId <your-project-id>
```

## After changing rules or indexes

```powershell
./firebase/setup.ps1 -ProjectId <your-project-id> -SkipDatabase
```

## Local emulator (no cloud, no cost)

```powershell
./firebase/setup.ps1 -Emulator
```

Runs Auth and Firestore on localhost, with a web UI at http://localhost:4000. Starting it also compiles the rules, so it's the quickest syntax check.
