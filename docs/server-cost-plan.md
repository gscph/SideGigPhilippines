# Server cost plan: one year on almost nothing

Goal: run Tarbaho for a year of slow, organic growth (say up to ~1–3k monthly users) for a few dollars a month,
without making the app feel slower. Status of each item is tracked in the table at the end.

## Where the money goes today

| Piece | How it bills | Why it costs us |
|---|---|---|
| **Azure SQL (serverless)** | per second while the database is awake (min ~0.5 vCore), then it pauses after the auto-pause delay | **The big one.** Any request that touches SQL wakes it, and it stays billed until the delay runs out. Today the app wakes it on purpose (`GET warmup` on every app open), every phone sends error reports every 10 min (`POST error-reports` → SQL), an open chat polls every 5 s, and the Seeker/Pro home refreshes every 15 s. A handful of users spread over the day can keep it awake ~24 h. Always-on 0.5 vCore is roughly **US$150–200 a month**; the free offer covers only ~55 awake hours a month. |
| **Functions (Flex Consumption)** | per execution + GB-seconds, after a monthly free grant | Cheap if the request count is low and instances are capped (2 GB instances, at most 2; the free grant is counted in GB-seconds). Every poll and refresh counts. |
| **Blob Storage (photos)** | storage + reads + egress | Pennies. Photos are already content-addressed with a 1-year immutable cache header. Full 1280 px photos on result cards waste egress. |
| **Application Insights** | per GB ingested | Can grow quietly with logs; needs a daily cap. |
| **Firebase Auth (phone SMS)** | per SMS sent (Blaze plan); Google sign-in and FCM are free | SMS is the only real Firebase cost: phone is the first sign-in option for everyone, Resend has no cooldown, and without App Check bots can request codes (**SMS pumping fraud**). |

The idea, in one line: **reads come from files, not from the database; the database wakes only when someone
writes something; anything not urgent goes through a queue.**

## Recommendations (what we'd do and why)

**Caching: yes, but no Redis.** Redis costs ~US$16+/month just to exist. Instead:
- Phone side: longer freshness windows, `ETag` → `304` on everything that is re-read, refresh on pull and on push, not on a timer.
- Server side: precompute what Seekers browse into **static JSON files in Blob Storage** (public, gzipped, short
  `Cache-Control`). The phone reads them straight from Blob: no Function, no SQL. Search is the hottest path; with 3
  towns and ~10 trades it is ~30 small files.

**Photos on the server: yes, for workers.** They are the worker's proof and the anti-fraud hash check needs them
server-side. Keep them in Blob (already), add a small **thumbnail** (≈320 px) made at upload for cards and lists, so
the 1280 px photo loads only on the profile. Seeker photos: use the Google account photo URL when there is one.

**Service Bus: no. Azure Storage Queues: yes.** Service Bus Standard has a monthly base fee and we don't need its
features (topics, sessions, ordering). Storage Queues live in the storage account we already pay for, cost
fractions of a cent per 10k messages, and Functions has a built-in queue trigger. Deferred work:
rebuilding the search/profile files after a listing or review changes, sending push notifications, making
thumbnails, the nightly clean-ups.

**Error reports and other write-only telemetry off SQL.** Error reports, "Others" job notes and feedback votes don't
need a relational database. Put error reports and job notes in **Azure Table Storage** (same storage account,
pennies), and send them on launch and when the app goes to the background, not every 10 minutes.

**Chats: push, not polling.** We already have FCM. When a message is written, send a silent data push to the other
person; an open chat fetches only then. Polling drops to a 60 s safety net. This alone removes ~95% of chat requests.

**Accept the cold start, don't pay to avoid it.** Remove the database part of the warmup. Once browsing reads files,
the only thing that waits for a paused database is a write (send, publish, review), behind the existing loading
screen.

**SMS codes: once in a person's lifetime (owner decision).** The number is verified once, and from then on the person
signs in without SMS. A Firebase sign-in already lasts until sign-out or reinstall (no re-verify); a new phone or a
reinstall signs back in with Google, or Apple on iOS, where the phone's own PIN, fingerprint or Face ID does the
unlocking. So:
- **Everyone signs in with Google (Apple on iOS).** The phone-number sign-in leaves the sign-in screen.
- **Workers verify their number first, once:** right after choosing "Skilled Worker", before the interview (anti-fraud
  rule 1; the number is linked to the Google/Apple account, so the uid stays the same). The server records when the
  account's number was verified (`Users.PhoneVerifiedAt`), and the app never asks that account again: not after a
  reinstall, a new phone or an edit.
- **Seekers are never asked for a number.**
- **Resend waits 360 s,** and at most 3 codes a day per phone (remembered on the device), with a friendly message
  after that.
- **Server/Firebase side:** App Check (Play Integrity) on Auth and the PH-only SMS region list (item 0), plus a daily
  SMS quota alert, so bots can't send codes on our bill.
- Changing the number later (rare) is the only other SMS, from the Me page.

**Guardrails so a bug or an attack can't run up a bill:** budget alerts, a cap on Function instances, an Insights
daily cap, Firebase App Check + SMS region allow-list (PH only).

## Work plan

Each item is **one branch from `main`, one PR**, merged with **Squash and merge** so `main` reads as one commit per
feature (`perf(chat): push instead of polling`). Items are ordered so the cheap, safe wins land first; ones marked
*after N* change the same files as item N and wait for it to merge.

| # | Branch | What | Why it saves |
|---|---|---|---|
| 0 | *(portal / script, you run it)* | `azure/cloudshell-cost-guardrails.sh`: SQL serverless max 1 vCore, auto-pause delay at the minimum (or move to the SQL free offer); Function App instance memory 2 GB, max instances 2, no always-ready; Insights daily cap + sampling; Azure budget (alerts at US$5/10/20); Firebase: budget alert, App Check (Play Integrity) on Auth, SMS regions = PH | Caps the worst case on day one; no code |
| 1 | `fix/crash-capture-offline` | The crash capture and offline work already done (uncommitted now) + this plan | — |
| 1b | `auth/sms-once` | Sign-in with Google (Apple on iOS) first, the SMS code behind a link for phone-only accounts; workers verify their number once, right after choosing the worker role (the number stays on the sign-in account, so it is never asked again); Seekers never; Resend after 360 s, max 3 codes a day per phone; tests for the rules | One SMS per worker, ever; none for Seekers |
| 2 | `perf/error-reports-table-storage` | Error reports and "Others" notes → Azure Table Storage; send on launch / going to background, not every 10 min; the read endpoint reads Table Storage | Every phone stops waking SQL every 10 min |
| 3 | `perf/app-refresh-windows` | Home/Inbox refresh on open, pull and push instead of every 15 s; `ETag`/`304` on profile, inbox and me reads; longer listing freshness | Fewer requests per session |
| 4 | `perf/chat-push-not-poll` | Silent FCM data push on new message; the open chat and Inbox refresh on it; poll fallback 60 s | Removes most chat traffic |
| 5 | `perf/storage-queue` | Storage Queue + queue-triggered function; move push sending and nightly clean-ups to it; timer function once a day | Requests return faster; work batches |
| 6 | `perf/static-search-files` (*after 5*) | On publish/remove/review a queue message rebuilds per-trade search files and the worker's public profile file in Blob; the app reads search and profiles from Blob (falls back to the API if a file is missing); blocks are still applied (blocked ids filtered on the phone from the person's own block list) | **Seeker browsing stops touching Functions and SQL** |
| 7 | `perf/no-db-warmup` (*after 6*) | Warmup no longer runs `SELECT 1`; drop it if nothing else needs it | The database sleeps between writes |
| 8 | `perf/photo-thumbnails` (*after 5*) | 320 px thumbnail made at upload (queue), cards and lists use it | Less egress, faster lists on 4G |
| 9 | `perf/request-budget-tests` | Tests that count API calls for typical sessions (open app, search, open profile, chat) and fail when they grow; a short "what to watch" section in `azure/README.md` | Keeps the savings from creeping back |

Rough result: **SQL awake only around writes** (minutes a day early on, inside or near the free offer), Functions
well inside the free grant, Storage and Queues a few cents, Firebase only the SMS sent. Real numbers are checked
after items 2, 4 and 6 with the portal's metrics (SQL "billed vCore", Function executions).

## How the 30-minute loop works (after you approve)

Each tick: fetch `origin`, take the next item whose dependencies are merged, branch from `origin/main`, implement,
build-check + tests in a throwaway copy, commit (conventional message, why in the body), push, open the PR, mark it
here. **One PR per tick; never merges, never pushes to `main`, never force-pushes.** New SQL schema scripts go in as
guarded numbered scripts and still need your approved deploy run. If a PR gets review comments, the next tick
answers them first. When every item is done, the loop stops itself.

## Status

| # | State | PR |
|---|---|---|
| 0 | PR open (#2) | `chore/cost-guardrails` |
| 1 | merged (#1), review fixes PR #3 | `fix/crash-capture-offline`, `fix/offline-review` |
| 1b | PR open | `auth/sms-once` |
| 2 | not started | — |
| 3 | not started | — |
| 4 | not started | — |
| 5 | not started | — |
| 6 | not started | — |
| 7 | not started | — |
| 8 | not started | — |
| 9 | not started | — |
