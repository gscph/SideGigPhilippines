# MobileAppTheme (reusable app skeleton)

This project is the shared skeleton every mobile app in this repo builds on: shell, tabs, theme
builder, generic components, English/Taglish localization and on-device adapters. Inventory and
"start a new app" steps: `README.md`.

**Load the `mobile-app-theme` skill before changing anything here.** In short:

- The skeleton never depends on an app: no `ProjectReference`, no `SideGigPh` namespace, no app `Copy`, no app routes.
- No domain words (Professional, Seeker, listing, inquiry, peso, towns…). Domain data comes in through `[Parameter]`s; app-specific wording through optional `string?` parameters that fall back to `CommonCopy`.
- Brand only through `AppBrand` / `BrandStyle`; no hex outside `Branding/BrandStyle.cs`.
- New capability = port in `Services/` + on-device adapter in `Services/Local/`, registered with `TryAddSingleton` in `AddMobileAppTheme`.
- Same house rules as the apps: `.razor` + `.razor.cs` code-behind, MudBlazor only, no `.razor.css`, no dead code.
- Public types, parameters, `CommonCopy` keys and CSS classes are used by every app: add, don't rename. A breaking change updates every app in the same change.
- Run `bash .claude/skills/mobile-app-theme/scripts/skeleton-check.sh` and the `build-check` skill before calling a change done; update `README.md` and the root `CLAUDE.md` inventories.
