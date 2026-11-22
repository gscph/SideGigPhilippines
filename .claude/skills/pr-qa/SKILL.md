---
name: pr-qa
description: QA every SideGigPh pull request before handing it to the owner. Run it ALWAYS, right after opening a PR (and again after pushing fixes to one), and whenever the user asks to QA, review or "run a QA agent" on a PR. Starts a QA agent that checks the PR's own changes first, then the core features (onboarding, search, job posts and offers, chat) on Android and iOS, runs the tests and build checks, then posts one QA comment on the PR. Replaces the old GitHub "Claude review" workflow.
---

# pr-qa

The owner wants every PR checked before they look at it: **the PR's own changes first, then the core features,
on both Android and iOS**. GitHub runs no tests or reviews any more (its workflows are manual builds: iOS to
TestFlight, the Android AAB, the API deploy), so this local QA is the only check a PR gets before the owner merges.
The tests run here, never on GitHub.

## When

- **Always, right after you open a PR** (`gh pr create`). Don't tell the owner the PR is ready before QA is done.
- After pushing fixes to an open PR: run it again, briefed on what changed since the last QA.
- When the user asks to QA / review a PR (theirs or yours).

## Steps

1. **Version label** (it used to be the GitHub review's job): when you open the PR, label it `minor` for a change
   people using the app will notice, `major` only for a breaking change or milestone, nothing for anything smaller
   (`gh pr edit <n> --add-label minor`). Never change a label the owner set.
2. **Start the QA agent** with the Agent tool (`subagent_type: general-purpose`, fresh, not a fork, so it reads the
   PR with new eyes). Fill in the brief below: PR number, branch, worktree/folder, and one line on what the PR is
   for. Several PRs: one agent each, in parallel.
3. **A device is required when the PR changes the app** (SideGigPh.Maui, MobileAppTheme*, .razor, csproj). Before
   starting the agent, run `adb devices`. If it lists nothing, ask the owner to start the emulator from Visual Studio
   or plug in a phone with USB debugging (never start an emulator yourself), and start QA once one is listed. QA that
   could not run the app on a device is **not done**: say "QA not done: no device" on the PR and to the owner, never
   "no problems found". Reading code and unit tests miss what only a phone shows (the camera, a dropped connection,
   "you're offline" while online, a Release-only crash), and those reached testers on 22 Nov 2026.
4. **While it runs**, don't touch the PR's branch.
5. **Read its report critically.** Check each finding against the code yourself before acting; drop what you can't
   confirm. A finding is a fact with `path:line` and a failure scenario, not a worry.
6. **Fix confirmed problems** on the PR's branch (root cause, never weaken a test), push, and run a short second
   QA pass on the fix only.
7. **Post one QA comment** on the PR (`gh pr comment <n> --body-file -` with a quoted heredoc), shape below. On a
   re-run, post a new short comment ("QA after fixes"), don't edit the old one.
8. **Tell the owner** in plain words: verdict, what was fixed, and exactly what was **not** checked (e.g. iOS not
   run on a device). Never say "works on iOS" when only the code was read.

## The QA agent brief (copy, fill the <…>)

```
You are QA for SideGigPh PR #<n> ("<title>"), branch <branch>, checked out at <folder>.
Purpose of the PR: <one line>.
Do NOT edit, commit or push anything. Report back; the main session fixes.

Read CLAUDE.md first (hard rules), then the sections of docs/architecture.md for the areas the PR touches.
Get the change: `gh pr view <n>`, `git diff origin/main...<branch>`. Read the surrounding code and callers.

Check in this order and report in these sections:

1. THE PR'S CHANGES, file by file: is each change correct on its own? Wrong logic, null/empty cases, async misuse,
   a contract/JSON field or ApiErrors code the other side still expects, a missing [JsonSerializable] line in
   ApiJson, a server rule no longer enforced, a schema change that isn't a new guarded NNN-schema-*.sql recorded in
   SchemaChanges + SchemaScripts.Required (001 never edited, SideGigPhDb.cs matching), copy not in Copy.cs as an
   English/Taglish pair, raw HTML or hex in .razor, a tap handler without the `if (_busy) return;` guard.

2. CORE FEATURES: follow the code paths the PR touches and say for each whether it still works:
   - Onboarding: language -> sign in (phone first, Google; Apple on iOS) -> role -> interview, one URL per step
     (Services/OnboardingSteps, LaunchRouter, resume at launch); a worker's trade -> jobs -> price -> towns ->
     experience -> selfie -> number -> preview -> promises -> Ready/publish; a Seeker ending on search-or-post.
     Onboarding never loops or blocks on a missing option ("Others" always finishes).
   - Search (relevance): search sheet, results (SearchRanker: exact job, your town then nearby, over budget lower
     but never hidden, "Others" text matching), SearchIntentParser / SearchText, profile -> Contact -> chat.
   - Job posts and offers: post form (JobBoard, PostReview, its sheets' URLs), the worker's Requests feed,
     offers, Pick -> chat, JobAlerts; numbers private until a pick, the household's point never sent.
   - Chat: both Inboxes (InboxPages), open a chat (ChatHistory), send/reply, unread, notifications
     (ChatAlerts), PickedJobCard (Mark done, Cancel), Call/Text only after a pick, report, review.
   If a core feature's expected behavior is unclear, say so instead of guessing.

3. PLATFORMS, Android and iOS:
   Use this PR's own throwaway copies, so parallel QA agents never share one:
   `export TEST_COPY="$TEMP/sidegigph-tests-pr<n>" BUILD_COPY="$TEMP/sidegigph-build-pr<n>"`.
   - Run the tests: `git fetch origin main`, then
     `bash .claude/skills/run-tests/scripts/run-tests.sh --changed=origin/main` (from <folder>). Plain
     `--changed` only sees uncommitted files and would skip every PR. "tests skipped" is right only when the PR
     touches nothing the tests see; say which files made that call.
   - Compile-check: `bash .claude/skills/build-check/scripts/build-check.sh` (Android app + API; run in the
     background, it takes minutes) when app or API code changed. Never build in the repo folder itself.
   - Android on a device, REQUIRED when the PR changes the app (SideGigPh.Maui, MobileAppTheme*, .razor, csproj):
     `adb devices` must list an emulator or phone (the main session made sure). Install from the build-check copy
     and walk, tapping through the real screens: the flows the PR touches, then onboarding as a worker through
     the selfie (take one with the camera, and once with "Choose from your photos") and Ready, a Seeker search,
     a job post with a photo, an offer, and a chat message. Leave the app in the background for 2+ minutes (open
     the camera or Home) and come back, then post or upload: a write after a pause must not say "offline".
     Watch for "you're offline" or "No internet" on a working connection: that is a bug, never a pass. build-check's Debug build can't be installed as it is (Fast Deployment "No
     assemblies found" crash at launch): in $BUILD_COPY run `dotnet build -t:Install -f net10.0-android -c Debug
     -p:EmbedAssembliesIntoApk=true "-p:AdbTarget=-s <device>"` on SideGigPh.Maui (background, minutes; the
     build-check script shows how it reaches the copy through a short drive alias), then launch it. Never
     start an emulator or adb server yourself; if none is running, stop and report "QA not done: no device".
     When the PR touches Release-only settings (csproj PropertyGroups for Release, R8/proguard, a new Android
     library), also install a Release APK (`dotnet publish -f net10.0-android -c Release` in $BUILD_COPY, then
     `adb install -r` the signed APK) and walk sign-in, the selfie and a post on it: R8 breaks only Release builds.
     After the walk: `adb logcat -d | grep -E " E (SideGigPh|AndroidRuntime|chromium)"` for errors, and list every
     error the app logged (each one is an error report testers would send).
   - iOS can't be built or run on this Windows PC. Check it by reading: everything under
     SideGigPh.Maui/Platforms/iOS (Info.plist, Entitlements.plist, AppDelegate, SceneDelegate), every
     `#if ANDROID` / `#if IOS` the PR adds or touches (does iOS get a working path, not a silent no-op that
     blocks a flow?), platform-conditional items in the csprojs, MobileAppTheme.Firebase's iOS paths (sign-in,
     Apple sign-in, phone link answering PhoneInUse, push), safe areas / keyboard, and anything that calls an
     Android-only API from shared code. Write "iOS: code read, not built or run" in the report.

Every finding needs EVIDENCE: path:line, the few lines quoted, and a concrete scenario (who does what, what goes
wrong, on which platform). Severity: Breaks a core feature > Bug > Hard rule broken > Risk. No style nits.

Return:
## The PR's changes   (findings, or "No problems found")
## Core features      (one line each: Onboarding / Search / Job posts and offers / Chat: OK or the finding)
## Android            (tests, build, device run or why not)
## iOS                (what was read, findings, "not built or run")
## Not checked        (everything you could not verify)
```

## The PR comment

```
## QA
One line: what the PR changes and the verdict ("No problems found" / "N problems found, M fixed").

### The PR's changes
### Core features
Onboarding: … · Search: … · Job posts and offers: … · Chat: …
### Android
Tests: N passed · Build: OK · Device: walked <flows> on <device> (Debug / Release) · or "QA not done: no device"
### iOS
Code read (<what>) · not built or run on this PC
### Not checked
```

Findings inside a section: `**1. <Severity>: <title>**`, `path:line`, the evidence, and "Fixed in <sha>" or the fix
to make. Plain words, no jargon the owner wouldn't use.
