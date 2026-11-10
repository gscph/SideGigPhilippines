---
name: build-check
description: Compile-check the SideGigPh MAUI app after code changes without interfering with Visual Studio. It builds a throwaway copy and works around Android's long-path (aapt2 APT2098) failure. Use after editing .razor, .cs, .csproj or theme files, before saying a change "builds", or whenever you want to know whether MudBlazor parameter or enum names are right. Not for deploying or running the app.
---

# Build check (safe alongside Visual Studio)

The owner builds, deploys and runs the app from **Visual Studio**. Building in the real `SideGigPh.Maui/` folder from the CLI fights VS over `bin/obj` (file locks, cancelled builds). Launching the emulator or using `adb install` from the CLI makes VS hang on "waiting for emulator". So:

- **Never** run `dotnet build`/`publish`/`-t:Run` in the repo folder, launch the emulator, or deploy with adb.
- **Do** compile a copy with the script below, then tell the user to deploy from VS to see it running.

## Run it

```powershell
pwsh -NoProfile -File .claude/skills/build-check/scripts/build-check.ps1 -WorkDir "<session scratchpad>\bc"
```

- Pass the session's scratchpad as `-WorkDir`. The script refuses a folder inside the repo.
- It copies `SideGigPh.Maui` without `bin/obj` and maps the copy to a free drive letter with `subst`. That keeps paths short, because aapt2 fails with `APT2098 … failed to open file` / `APT2261` on long Windows paths, for example inside `AppData\Local\Temp\claude\…`. It then builds `net10.0-android` Debug, prints errors plus relevant warnings (`RZ`, `BL`, `MUD`, nullable `CS8xxx`), unmaps the drive and deletes the copy.
- Output ends with `BUILD OK …` (exit 0) or `BUILD FAILED …` (non-zero).
- A cold run restores packages and takes a few minutes; with a warm NuGet cache it takes about a minute. Run it in the background when you have other work to do meanwhile.
- `-Keep` leaves the copy for a faster re-run. `-Framework`/`-Configuration` change the target. iOS can't build on Windows, so CI on macOS covers iOS.

## Reading failures

- `APT2098` / `APT2261` on `common_google_signin_btn_*`: a path-length problem, not your code. Check that the build ran from the `subst` drive.
- `RZ10012` (unknown component) or `RZ9991`: a missing `@using` in `Components/_Imports.razor`, or a misspelled component name.
- `CS0117` / `CS1061` on a MudBlazor type: wrong parameter or enum name for MudBlazor 9. Look it up with `bash .claude/skills/mudblazor-ui/scripts/mud-api.sh params <Component>`.
- `MUD0002` analyzer warning: an attribute MudBlazor doesn't recognize on a Mud component. Usually a PascalCase name that isn't a real parameter.
- If something fails only in the copy, check whether the file is gitignored and missing. Firebase config files are optional; the csproj includes them with `Exists(...)`.

## After it passes

A passing build proves the code compiles, not that it looks right. Tell the user what you could not see (layout, spacing, colors on a device) and leave the visual check to their VS deploy.
