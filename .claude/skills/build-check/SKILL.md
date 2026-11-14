---
name: build-check
description: Compile-check SideGigPh (the Android app SideGigPh.Maui and the API SideGigPh.Api) in a throwaway copy after code changes, safe while Visual Studio has the repo open. Use after changing C#, .razor, .csproj or MudBlazor markup, before saying the work is done. Never build, run or deploy in the repo folder from the CLI.
---

# build-check

The user builds and deploys from Visual Studio. A CLI build in the repo folder fights VS over `bin/obj`
(cancelled builds, locked files, "emulator not ready"), so every compile check runs in a copy:

```bash
bash .claude/skills/build-check/scripts/build-check.sh          # app (Android) + API
bash .claude/skills/build-check/scripts/build-check.sh --app    # only the Android app
bash .claude/skills/build-check/scripts/build-check.sh --api    # only the API
```

- The copy is `$BUILD_COPY` (default `$TEMP/sidegigph-build`); bin/obj stay there, so later runs are incremental.
  The first Android build takes a few minutes: run it in the background.
- On Windows the app builds through a short `subst` drive alias. Building from a long path makes aapt2 fail on
  the Firebase resources (APT2098). The script handles it; don't mention it to the user.
- It prints only errors and the result line. Warnings are expected (3 on Android today).
- Tests are a separate step: the `run-tests` skill. A passing build doesn't prove the UI looks right.
- Never `dotnet build`/`run`/`publish`, `adb install` or start an emulator in the repo folder.
