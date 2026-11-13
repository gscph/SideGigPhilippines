---
name: run-tests
description: Run SideGigPh's automated tests (SideGigPh.Tests, unit + integration) after a significant change to code they cover, and fix what they catch at the root cause. Use after changing anything in SideGigPh.Maui/Services or Models, SideGigPh.Api, SideGigPh.AzureSQL (including Sql/ scripts), SideGigPh.Contracts, MobileAppTheme C# code, a .csproj, or the tests themselves; and before saying such work is done. Do NOT use for CSS-only, image, docs or pure .razor-markup changes (the script's --changed check skips those). Never weaken, skip or delete a test to make it pass.
---

# Run the tests, then fix the cause

SideGigPh.Tests protects three promises to users: no major bug after an update, no change that locks someone out of
the app, no change that makes it slow. `SideGigPh.Tests/README.md` says what each folder protects.

## When to run

| Changed | Run? |
|---|---|
| `SideGigPh.Maui/Services/**`, `Models/**`, `Components/**/*Routes.cs` | yes |
| `SideGigPh.Api/**`, `SideGigPh.AzureSQL/**` (incl. `Sql/*.sql`), `SideGigPh.Contracts/**` | yes |
| `MobileAppTheme/**/*.cs`, any `.csproj`, `SideGigPh.Tests/**` | yes |
| `.css`, images, `docs/`, `.md`, `.razor` markup only | **no** |
| A `.razor.cs` page with new rules in it | move the rule into `Services/` first (see below), then yes |

`run-tests.sh --changed` makes this call for you from `git diff`: it exits 0 with "tests skipped" when nothing the
tests can see changed. CI (`.github/workflows/tests.yml`) uses the same paths, so CSS-only pushes don't run it.

## How to run

Never build in the repo folder (Visual Studio may have it open). The script copies the source to a temp folder and
runs there:

```bash
bash .claude/skills/run-tests/scripts/run-tests.sh --changed     # usual: skips when only styling/docs changed
bash .claude/skills/run-tests/scripts/run-tests.sh --unit        # quick loop while fixing (no database)
bash .claude/skills/run-tests/scripts/run-tests.sh --coverage    # all tests + coverage per file + every missed line
bash .claude/skills/run-tests/scripts/run-tests.sh --filter "FullyQualifiedName~Publishing"
```

Integration tests need SQL Server: LocalDB on Windows (comes with Visual Studio), or a connection string in
`SIDEGIGPH_TEST_SQL`. Without one they are **skipped, not passed**: say so in your report, never claim they ran.

After changing app code under `SideGigPh.Maui`, also compile-check the Android app (the tests compile only its
Services/Models): copy the projects without bin/obj to a short path and `dotnet build -f net10.0-android`.

## When a test fails: fix the code, not the test

1. **Read the failing test**: its name and the comment above its class say which user promise broke
   (e.g. `Publish_ReferenceHasWorkersOwnNumber_ReturnsBadReferenceWithoutSending`). Read the assertion message.
2. **Decide which side is wrong.**
   - The change broke behavior users rely on → fix the app/server code.
   - The user *asked* for this behavior to change (e.g. "allow 4 references") → update the test to the new
     rule, and say which test you changed and why.
   - Unsure → ask; don't guess.
3. **Fix the root cause.** Forbidden "fixes":
   - deleting, skipping (`Skip =`), commenting out or loosening an assertion (`BeLessThan` limits, `ContainSingle` → `NotBeEmpty`);
   - special-casing test values in app code (`if (uid == "worker-uid")`), catching and hiding exceptions to turn red green;
   - editing a snapshot list to match a rename. `UpdateSafety/StableIdsTests` and `WireContractTests` fail **on
     purpose** when an id, JSON field name or error code changes: phones running the old version break. Change
     the label instead, or add a new field next to the old one. Only edit the snapshot when the user explicitly
     accepts that old app versions break, and say so.
   - raising a performance budget in `Performance/` because the code got slower: find the slowdown.
4. **Check you didn't break something else**: re-run the **whole** suite (not just the failed test), and the Android
   compile check if app code changed. A fix that turns one test green and another red is not done.
5. **Report** what failed, the cause, the fix, and the final counts (passed / skipped). Skipped integration tests
   must be mentioned.

## When you add or change behavior

- New logic needs tests in the matching `Unit/` folder (one behavior per test, `Method_Scenario_Expected` name,
  Arrange/Act/Assert, a one-line *why* where it isn't obvious). A new stored field or route also needs an
  `Integration/` test that goes phone → API → database and back.
- New app files under `SideGigPh.Maui/Services/` or `Models/` are compiled into the tests automatically; a new
  route class needs a `<Compile Include=…>` line in `SideGigPh.Tests.csproj`.
- Keep page rules testable: decisions (which step, what an answer clears, where to go) belong in a service such as
  `Services/OnboardingSteps.cs`; the `.razor.cs` only calls it and navigates. Pages aren't compiled by the tests.
- New contract type → `[JsonSerializable]` in `ApiJson` (the `EveryContract_IsRegisteredInApiJson` test checks)
  and its field names in `WireContractTests`.
- Aim for 100% line coverage of the code you touched: `--coverage` lists every missed line. A line that can't run
  by design (a defensive `_ =>` arm) is fine; say which in your report.

## Scripts

- `scripts/run-tests.sh`: copy + run (`--changed`, `--unit`, `--coverage`, extra `dotnet test` args).
- `scripts/coverage-summary.awk`: per-file line coverage from the Cobertura report (CI uses it too).
- `scripts/uncovered-lines.awk`: every line the tests never ran (`path:line`).
