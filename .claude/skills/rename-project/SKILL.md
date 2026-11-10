---
name: rename-project
description: Safely rename this MAUI project or its root namespace (e.g. SideGigPh → SideGigPh.Maui), touching every place the name lives so nothing silently breaks. Use when renaming the project/folder/assembly/namespace, or when a build fails with CS0234 / "type or namespace does not exist" / a missing component, or component styles stop loading, after such a rename.
---

# Renaming the MAUI project / namespace

A MAUI Blazor Hybrid rename touches more than the `.csproj`. Miss one spot and you get a compile break (CS0234) or — worse — a silent runtime bug (scoped CSS stops loading). Work the full checklist, then grep to prove it's clean.

Current layout: solution `SideGigPh.slnx` at repo root → project `SideGigPh.Maui/SideGigPh.Maui.csproj`, root namespace & assembly name `SideGigPh.Maui`. **The solution file keeps the product name (`SideGigPh`); only the project is `SideGigPh.Maui`.**

## Checklist — every place the name appears

1. **Project file** — rename `Old.csproj` → `New.csproj`; set `<RootNamespace>New</RootNamespace>`. If `<AssemblyName>` is set explicitly, update it too; if it's absent, the assembly name defaults to the csproj filename (this matters for #6).
2. **Project folder** — rename the folder. See "Folder-lock workaround" below; a MAUI folder is almost always locked.
3. **All `.cs` files** — `namespace Old` → `namespace New`. The **Windows shell keeps a `.WinUI` suffix**: `Platforms/Windows/App.xaml.cs` is `namespace New.WinUI`.
4. **All `.xaml` files** — `x:Class="Old.…"` → `New.…`, and `xmlns:local="clr-namespace:Old"` → `clr-namespace:New`. Any other `clr-namespace:Old.Foo` (e.g. `xmlns:components="clr-namespace:Old.Components"`). WinUI XAML uses the WinUI form: `x:Class="New.WinUI.App"`, `xmlns:local="using:New.WinUI"`.
5. **`.razor` imports — the #1 missed spot** → `Components/_Imports.razor`. Update every `@using Old…` to `@using New…` (`@using Old`, `@using Old.Components`, `@using Old.Components.Layout`). This is the usual cause of `CS0234 … namespace does not exist` at build time, because every component inherits these usings.
6. **`wwwroot/index.html` — silent runtime trap** → the scoped-CSS bundle is named `<AssemblyName>.styles.css`. After the rename it must be `<link rel="stylesheet" href="New.styles.css" />`. If left stale, the build still succeeds but every component `.razor.css` stops loading — styling silently disappears. (In this repo scoped CSS is avoided per `mudblazor-ui`, but the bundle link must still match the assembly name.)
7. **Solution file `.slnx`/`.sln`** — update `<Project Path="…">` to the new csproj path (`SideGigPh.Maui/SideGigPh.Maui.csproj`). Keep any `<Deploy Solution="Debug|*" />` so Android debug-deploy still works from VS. Don't rename the solution file unless the product name itself changes.

## Folder-lock workaround

Visual Studio (`devenv`) locks the project folder, so `git mv` / `Rename-Item` fail with "being used by another process":
1. Close Visual Studio first.
2. If it still fails, the session's own working-directory handle may hold the lock. Move the contents with robocopy, which copies then removes sources:
   ```powershell
   robocopy OldFolder NewFolder /E /MOVE
   ```
   `/MOVE` may leave the empty source dir behind if a stale handle remains — it clears on reboot; don't fight it.
3. Then point the session's working directory away from the old folder (reopen at the repo root) so later `git`/build commands don't re-lock it.
4. Stage as renames so history is preserved: `git add -A` then confirm `git status` shows `R` (rename) entries.

## Verify — prove it's clean

Grep the whole tree for the **bare old name** across code + markup:
```bash
grep -rnE "\bSideGigPh\b" --include=*.cs --include=*.xaml --include=*.razor --include=*.html --include=*.csproj --include=*.slnx .
```
Also specifically check razor usings: `grep -rn "@using SideGigPh" --include=*.razor .`

The only legitimate remaining hits are **display strings**, not namespaces:
- `ApplicationTitle` in the csproj, `Window … Title = "SideGigPh"`, `<title>SideGigPh</title>` in index.html — these are the app's shown name and stay.
Everything else (namespaces, `x:Class`, `clr-namespace`, `@using`, csproj `<Project Path>`, the `.styles.css` link) must be the new name.

## Before you finish

- Run the verify greps above; confirm only display strings remain.
- Build in CI (push) rather than locally — see the `maui-ci` skill. CS0234 shows up there if a namespace was missed.
- Update `CLAUDE.md` (target solution structure, any path/name references) to match the new layout.
