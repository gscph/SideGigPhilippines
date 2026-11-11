# firebase/

Server-side setup for SideGigPh's Firestore and Storage (MVP). The data model is [`docs/data-model.md`](../docs/data-model.md).

| File | What it is |
|---|---|
| `firestore.rules` | Who may read/write each collection, and which fields and values are allowed. Firestore has no server in front of it, so the rules act as the API and the schema. |
| `firestore.indexes.json` | The composite indexes the Inbox (chats) and review lists need. Search uses only equality filters, which need no composite index. |
| `storage.rules` | Photo upload/download rules (work photos and profile photo). |
| `firebase.json` | Ties the above together; emulator ports. |
| `setup.ps1` | Creates the database and deploys everything. |

## First time

1. Firebase console: put the project (the one whose `google-services.json` the app uses) on the **Blaze** plan (needed for Storage).
2. Firebase console → **Storage → Get started**, region **asia-southeast1** (same as Firestore).
3. Install the CLI and log in:

```powershell
npm install -g firebase-tools
```

```powershell
firebase login
```

4. Create the database and deploy:

```powershell
./firebase/setup.ps1 -ProjectId <your-project-id>
```

The first Storage deploy asks to let Storage read Firestore (needed for the duplicate-photo check). Answer yes.

## After changing rules or indexes

```powershell
./firebase/setup.ps1 -ProjectId <your-project-id> -SkipDatabase
```

## Local emulator (no cloud, no cost)

```powershell
./firebase/setup.ps1 -Emulator
```

Runs Auth, Firestore and Storage on localhost, with a web UI at http://localhost:4000. Starting it also compiles the rules, so it's the quickest syntax check.
