# Legal pages and store readiness

The three public pages, what the app does to meet the Play Store and App Store rules, and what is still left for you.
**They were drafted without a Philippine lawyer.** Have one review them before launch (see "Still to do").

| Page | File (the one source) | Public address (served by the API, `GET legal/{page}`) |
|---|---|---|
| Privacy Policy | [`privacy-policy.md`](privacy-policy.md) | https://sidegigphapi-hrb9bgerbgcgawbm.southeastasia-01.azurewebsites.net/api/legal/privacy |
| Terms of Service | [`terms-of-service.md`](terms-of-service.md) | https://sidegigphapi-hrb9bgerbgcgawbm.southeastasia-01.azurewebsites.net/api/legal/terms |
| Your data / delete your account | [`data-handling.md`](data-handling.md) | https://sidegigphapi-hrb9bgerbgcgawbm.southeastasia-01.azurewebsites.net/api/legal/data |

`SideGigPh.Api` embeds the three Markdown files and serves them as HTML (`LegalFunctions`), so editing a file and
pushing to `main` redeploys the page (`deploy-api.yml` watches them). They must stay free of `[placeholders]`
(`ApiWiringTests.LegalPages_AreWebPagesForAnyone`). The app links to the same addresses (`AppTheme.Legal`).

**Operator:** Joseph F. Cadiao, an individual doing business as Tarbaho; Data Protection Officer: the same; address
Unit 409, Tower C, The Hive Residences, Ortigas Ave. Ext., Brgy. San Isidro, Taytay, Rizal; one inbox for privacy,
support and legal. **Promise:** reports, data requests and deletion-by-email are answered within **5 business days**
(blocking works at once). Venue: the courts of Rizal with jurisdiction over Taytay. Liability cap ₱1,000.

## What the app does for the stores

| # | Rule | Where it is done |
|---|---|---|
| 1 | **Delete account** in the app (Apple 5.1.1(v), Google Play) | Me → Delete account (`MePage`, `ConfirmActionCard`) → `LocalAccount.DeleteAccountAsync` → `DELETE users/me/account` (`UserStore.DeleteAccountAsync`: listing, photos, any references stored before they were dropped, chats, reviews written **and received**, blocks, devices, profile; then the Firebase account through the Admin SDK, `FirebaseAccountDeleter`) → the phone deletes the Firebase account itself if the server couldn't, and signs out. Without the app: [`data-handling.md`](data-handling.md) §4 (the Play Console "Delete account URL") |
| 2 | **Report and block** (Apple 1.2, Google Play user-generated content) | 🚩 on a worker's profile, on each review, and in both chat screens → `ReportPage` (`/report/{side}/{subject}/{uid}`): reason, note, "also block" (on by default), or block only. `POST reports`, `PUT blocks/{uid}` (`SafetyStore`). A block either way hides the two from each other at once: search, profile (404), Inbox, new messages (`403 blocked`) |
| 3 | **Agree to the Terms and Privacy Policy** at sign-in | `LegalConsent` under the sign-in buttons ("By continuing, you agree to our Terms of Service and Privacy Policy", both linked) |
| 4 | **Policy links in the app** | Me (both sides) → "Privacy and terms": the three pages |
| 5 | **Sign in with Apple** next to Google (Apple 4.8), iOS only | `AppleSignInButton`, `FirebaseIdentityProvider.SignInWithAppleAsync`, `Platforms/iOS/Entitlements.plist` |
| 6 | ~~Reference consent~~ | **Gone with references** (dropped November 18, 2026): the app no longer asks for third people's details. Rows stored before then are read by nothing and purged by Delete account (Privacy Policy §3.4) |
| 7 | **Act on reports** within 5 business days | `dbo.Reports` (query below) |
| 8 | **Public policy pages** | The API route above |

### Reading and closing reports

```sql
-- Open reports, oldest first, with how many reports the same person has
SELECT r.Id, r.CreatedAt, r.Kind, r.Reason, r.Note, r.TargetUid, r.SubjectId,
       (SELECT COUNT(*) FROM dbo.Reports x WHERE x.TargetUid = r.TargetUid) AS ReportsAboutThem
FROM dbo.Reports r WHERE r.Status = 'open' ORDER BY r.CreatedAt;

-- After acting (e.g. removing a listing: DELETE dbo.Professionals WHERE Id = '<uid>'; or disabling the Firebase user)
UPDATE dbo.Reports SET Status = 'actioned', ResolvedAt = SYSDATETIMEOFFSET() WHERE Id = <id>;
-- Nothing wrong
UPDATE dbo.Reports SET Status = 'dismissed', ResolvedAt = SYSDATETIMEOFFSET() WHERE Id = <id>;
```

## Still to do (you)

1. **Run `SideGigPh.AzureSQL/Sql/011-schema-safety.sql` on the live database** (portal → Query editor) **before**
   pushing this API: the new API reads `Reports` and `Blocks` on every search.
2. **`FirebaseServiceAccount` on the Function App** (already used for notifications): without it the server can't
   delete Firebase accounts and the phone does it, which Firebase may refuse for an old sign-in.
3. **Sign in with Apple setup:** Apple Developer → the App ID → enable *Sign in with Apple*, regenerate the
   provisioning profile; Firebase console → Authentication → Sign-in method → enable **Apple**. Build iOS on a Mac
   (it can't be compile-checked on Windows).
4. **Store forms** (answers below), the support email on both listings, a **review test account** (Firebase →
   Authentication → Phone → a test number with a fixed code), age rating **18+**.
5. **A Philippine lawyer's review** of the three pages, especially whether a DOLE license question arises (Labor
   Code Art. 13(b)) and whether the Internet Transactions Act (RA 11967) needs DTI registration.
6. If you register a business name with DTI, add it to the operator line in all three pages.
7. Apple 1.2 also lists "a method for filtering objectionable material": a word filter on chats and reviews would
   close that fully; reporting and blocking are what reviewers usually check.

## Store forms

**Google Play → Data safety** and **App Store Connect → App Privacy** (no tracking, nothing used for ads, all
transferred encrypted, users can request deletion):

| Data type | Collected | Shared with third parties* | Purpose | Linked to user | Optional |
|---|---|---|---|---|---|
| Name | Yes | No | App functionality | Yes | No |
| Phone number | Yes | No | App functionality, fraud prevention | Yes | No |
| Email address | Yes (Google / Apple sign-in) | No | Account management | Yes | Yes |
| Photos | Yes | No | App functionality | Yes | Workers: no; Customers: yes |
| Messages (in-app chat) | Yes | No | App functionality | Yes | No |
| Other user content (listing, reviews, reports, "Others" text) | Yes | No | App functionality, safety, analytics (Others) | Yes | — |
| Crash logs / diagnostics | Yes | No | Analytics (app stability) | **No** | Yes (switch) |
| Device or other IDs (push token, random install id) | Yes | No | App functionality | Yes (push) / No (install id) | — |
| Location, contacts, financial info, browsing history, health | **No** | — | — | — | — |

\* Firebase and Azure are **service providers** processing on our behalf, which both stores count as "not shared".
When Google Analytics is added, add "App interactions / App activity: Analytics, not linked" and update the policy
§3.3 first; turn **off** advertising ID collection (`google_analytics_adid_collection_enabled = false`, and remove the
`AD_ID` permission the Firebase SDK merges into the Android manifest) so the Play form can say no advertising ID.

## Legal-risk review (Philippine law)

| # | Risk | Status |
|---|---|---|
| L1 | Employment / recruitment wording (Labor Code Art. 13(b)) | **Fixed:** role screen "Find a skilled worker or offer your services", worker tile "I offer my skills", tagline "Offer your skills…"; reports can flag "Offering a job or hiring workers"; Terms §3.4 bans employment posts |
| L2 | "Minimum wage" price floor (control over pay; NCR wage shown in Rizal) | **Fixed in copy:** shown as a "suggested minimum price"; the config label too. The code still names it `MinimumWage` (ids stay stable) |
| L3 | "Why you can trust them" | **Fixed:** "What we checked" |
| L4 | Third-party data (references) | **Removed:** references are no longer collected (Privacy Policy §3.4). Old rows stay until a later guarded script drops the `ProfessionalReferences` table (needs the owner's OK); Delete account purges them meanwhile |
| L5 | Internet Transactions Act (RA 11967) | Report/block and a reachable inbox cover the usual duties; counsel to confirm DTI registration |
| L6 | Errands that handle money | Terms put it on the users; consider a safety line on the Errands trade |
| L7 | Licensed trades (electrical, solar, refrigerants) | Terms §5(c) puts it on the worker |
| L8 | NPC registration | DPO named; register with the NPC when the user count makes it mandatory |

No clause shields us from our own **fraud, bad faith or gross negligence** (Civil Code Arts. 1171–1172) or from
Data Privacy Act duties. The Terms hold up by staying honest about what the app does: no dispatching, no payments,
no control over how work is done, no promises about workers.
