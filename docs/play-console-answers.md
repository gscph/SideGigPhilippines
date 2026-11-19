# Play Console: the answers to paste

What to answer in the Play Console's **App content** forms and the setup questions, so the first internal test
and the later review go through. The steps around them are in [`play-store.md`](play-store.md). The answers come
from the [Privacy Policy](legal/privacy-policy.md) and the app's Android manifest: **when either changes, change
this page and the form together.**

Checked against build `0.1.0` (versionCode 29, 19 Nov 2026): target SDK 36, min SDK 24, 64-bit only
(`arm64-v8a`, `x86_64`), native libraries 16 KB aligned, **no `AD_ID` permission**.

## Create app

| Question | Answer |
|---|---|
| App name | `Tarbaho` (the store listing may use the longer name in `store-assets/promo-text.md`) |
| Default language | English (United States) – en-US |
| App or game | App |
| Free or paid | Free |
| Category (Store settings) | House & Home |
| Contact email | the support email in the Privacy Policy (§1) |

## App access

"**All or some functionality is restricted**", then add one set of instructions:

- Name: `Test phone number`
- Username: the Firebase test number (Firebase → Authentication → Sign-in method → Phone → *Phone numbers for
  testing*, e.g. `+63 917 555 0100`)
- Password: its code (e.g. `123456`)
- Other information:
  > Choose English, then type the phone number above on the sign-in screen; the code is filled in or can be typed.
  > No SMS is sent. Choose "I need something done" to search, see worker profiles and post a request, or "I am a
  > Skilled Worker" to set up a worker listing. The app limits sign-in codes to 3 a day per phone, so please don't sign
  > out and in again many times on one device.

## Ads

**No**, the app has no ads.

## Content rating (IARC questionnaire)

- Category: **All other app types** (a directory; not a game, not social media).
- Violence, fear, sexuality, gambling, drugs, alcohol, tobacco, crude humour, bad language: **No** to every one.
- Users can interact or exchange content (chat, reviews, photos, posts): **Yes**.
- Users can share their location with other users: **No** (others see only a town and whole kilometres).
- Shares users' personal information with third parties: **No** (two people see each other's number only after a
  customer picks an offer; that is the users sharing, not us).
- Digital purchases: **No**.
- Unrestricted internet / web browser: **No**.

## Target audience and content

- Target age groups: **18 and over** only (Privacy Policy §2, Terms).
- Appeals to children: **No**.

## News, government, financial, health

- News app: **No**. Government app: **No**.
- Financial features: **My app doesn't provide any financial features** (households pay workers outside the app).
- Health apps: **No**.

## Advertising ID

**No**, the app does not use the advertising ID (no `com.google.android.gms.permission.AD_ID` in the manifest).
Check again when Firebase Analytics is turned on (`FirebaseAnalytics` build flag), since its SDK can add it.

## Privacy policy

`https://sidegigphapi-hrb9bgerbgcgawbm.southeastasia-01.azurewebsites.net/api/legal/privacy`

## Data safety

**Overview**

| Question | Answer |
|---|---|
| Does the app collect or share any of the required user data types? | **Yes** |
| Is all of the user data collected by your app encrypted in transit? | **Yes** (HTTPS only) |
| Which ways do you provide for users to request that their data be deleted? | **In the app** (Me → Delete account) **and a web link**: `https://sidegigphapi-hrb9bgerbgcgawbm.southeastasia-01.azurewebsites.net/api/legal/data` |
| Shared with third parties? | **No** for every type. Firebase and Azure are service providers acting for us, and two users seeing each other's number after a pick is a user's own choice; Play does not count either as sharing |

**Data collected** (none is sold; none is used for advertising or marketing; none is processed only ephemerally):

| Category → type | Collected | Required or optional | Purposes |
|---|---|---|---|
| Location → **Approximate location** | Yes | Optional (only on "Use my location"; the app asks for coarse too, though it drops a rough point) | App functionality |
| Location → **Precise location** | Yes | Optional (only on "Use my location") | App functionality |
| Personal info → **Name** | Yes | Required | App functionality, Account management |
| Personal info → **Email address** | Yes | Optional (Google sign-in only) | Account management |
| Personal info → **User IDs** | Yes | Required (the sign-in account id) | App functionality, Account management, Fraud prevention, security, and compliance |
| Personal info → **Phone number** | Yes | Optional (workers need one to publish; customers may skip) | Account management, Fraud prevention, security, and compliance, App functionality |
| Personal info → **Other info** | Yes | Optional (badge proofs: ID number, name and expiry read from a government ID; income goal) | Fraud prevention, security, and compliance, App functionality |
| Messages → **Other in-app messages** | Yes | Optional (chat) | App functionality |
| Photos and videos → **Photos** | Yes | Optional (profile and work photos, post photos, badge proofs) | App functionality, Fraud prevention, security, and compliance |
| App activity → **App interactions** | Yes | Required (a worker's last-active time, for "active recently") | App functionality |
| App activity → **Other user-generated content** | Yes | Optional (reviews, posts, offers, feature suggestions, "Others" text, reports) | App functionality |
| App info and performance → **Crash logs** | Yes | Optional (Me → Send error reports) | Analytics |
| App info and performance → **Diagnostics** | Yes | Optional (same switch) | Analytics |
| Device or other IDs → **Device or other IDs** | Yes | Required (push token, random install id in error reports) | App functionality |

**Not collected:** address, race, beliefs,
health, financial or payment info, contacts, calendar, SMS or call logs, audio, files, web history, installed apps,
and **references** (third people's names and numbers, no longer collected since 18 Nov 2026: untick them if an
earlier form listed them).

## Release notes (internal test)

```
<en-US>
First test build of Tarbaho: find skilled Kuya and Ate in Angono, Antipolo and Taytay, or offer your services.
Please try: sign in, search, a worker's profile, chat, post a request, and delete account.
</en-US>
```
