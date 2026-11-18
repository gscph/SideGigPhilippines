# SideGigPh.Tests

Automated checks that run in seconds and catch bugs before a user does. They protect three promises:

1. **No major bug when the app is updated.**
2. **No change that stops someone from using the app** (old phones talking to a new API, saved answers from an older
   version, a renamed trade or town).
3. **No change that makes the app slow or wasteful** (extra requests cost prepaid data and battery).

About 990 checks, 99.9% of the core code's lines (the 2 lines left can't run by design).

## Run them

**Visual Studio:** Test → Test Explorer → Run All. The integration tests use SQL Server LocalDB, which comes with
Visual Studio; each run creates its own throwaway database and deletes it after.

**Command line** (from the repo folder):

```bash
dotnet test SideGigPh.Tests
```

Only the fast ones (no database):

```bash
dotnet test SideGigPh.Tests --filter "Category!=Integration"
```

With coverage (which lines the tests ran):

```bash
bash .claude/skills/run-tests/scripts/run-tests.sh --coverage
```

That script builds a copy in your temp folder, so it never fights with Visual Studio over open files.

**On GitHub:** the *Tests* workflow runs on every push or pull request to `main` that changes code, and before every
API deploy (a failing test stops the deploy). Changes to only CSS, images, `.razor` markup or docs don't start it.

## What each folder protects

```
SideGigPh.Tests/
├── Unit/                 one piece at a time, no network or database (milliseconds each)
│   ├── Onboarding/       the interview: which question shows, what an answer clears, where the app opens, links
│   ├── Publishing/       publishing a listing: missing answers or proof, the server's own checks, an old app's
│   │                     body, and that the app and server agree on the rules (2–6 work photos)
│   ├── Search/           search box words (tubero → plumbing), result order (town, budget, stars), card wording
│   ├── Seeker/           the Seeker home: typing, tapping trades and towns, saved search
│   ├── Chats/            messages, calls and the unread badge on both sides; money maths
│   ├── Reviews/          only contacted workers can be reviewed, 1–5 stars, a new review replaces the old
│   ├── Photos/           picking and uploading photos (every failure shows a notice), server photo processing
│   ├── Account/          language, saved profile, Start over deletes everything, where sign-in lands
│   ├── PhoneNumbers/     every way Filipinos type a mobile number becomes +639…, and the server accepts it
│   ├── Adapters/         the app's HTTP layer: sends the token, reads every server answer, works offline,
│   │                     caches; the server's crash answers
│   ├── UpdateSafety/     things that break old phones if changed: trade/town/price ids, saved-answers file format,
│   │                     JSON field names and error codes, English + Taglish for every text
│   └── Performance/      ranking thousands of workers stays fast, screens don't re-ask the server needlessly
├── Integration/          the real API functions + a real SQL Server database + the app's real adapters, wired together
│   ├── WorkerToCustomerTests   publish → search → message → reply → review, end to end
│   ├── PublishingRulesTests    anti-fraud rules enforced by the server and database
│   ├── PhotosAndAccountTests   uploads (hash, owner, duplicates), profile, Start over
│   ├── ApiWiringTests          every route exists, sign-in required, privacy of chats, clear error codes
│   ├── ServerRefusalTests      bad or repeated requests, races, photo container setup
│   └── Support/                the in-memory API host, test database, test sign-in tokens, a phone per test person
└── TestSupport/          stand-ins for the phone's parts (fakes) and ready-made test data
```

## Reading a test

Each test name says what it checks: `Publish_MissingInterviewAnswer_ReturnsIncompleteWithoutSending`
reads as *method · situation · expected result*. Inside, three blocks: **Arrange** (set up), **Act** (do the one
thing), **Assert** (check). The comment above each class says why those tests matter to users.

## When a test fails

It found a change in behavior. Read its name and class comment: either the change is a bug (fix the code), or you
meant to change that behavior (then update the test, on purpose). Never delete or skip a test to get green.
The tests in `UpdateSafety/` fail **on purpose** when an id, JSON field or error code is renamed: phones running the
old version would break. Change the label instead of the id, or add a new field next to the old one.

## How the app code gets here

`SideGigPh.Maui` targets only Android and iOS, so this project compiles the app's `Models/` and `Services/` files
straight from their folders (see `SideGigPh.Tests.csproj`). Pages (`.razor`) are not compiled here, so page rules
live in services (e.g. `Services/OnboardingSteps.cs`) where they can be tested.
