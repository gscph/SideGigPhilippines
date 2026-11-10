---
name: mudblazor-ui
description: How to write or change UI markup in SideGigPh with MudBlazor 9 and no raw HTML — layout with MudStack, text with MudText, colors from AppTheme, verified parameter names, and the known gotchas. Use whenever you create, edit, restyle or review any .razor file, convert HTML to MudBlazor, change the theme/colors/fonts, or are unsure whether a MudBlazor parameter, enum value, icon or utility class exists.
---

# MudBlazor UI in SideGigPh

Every `.razor` file is built from MudBlazor components. No `<div>`, `<span>`, `<p>`, `<h1>`, `<button>`, `<input>`, `<ul>`, `<svg>`. The only raw HTML in the app is the pre-Blazor splash in `wwwroot/index.html`. Aim for 99% out-of-the-box MudBlazor; a workaround is allowed only when MudBlazor has no parameter or utility class for it.

## Never guess an API — look it up

MudBlazor changes names between majors (v9 is in use). Before using a parameter, enum value, icon or CSS utility you haven't verified in this repo, check it with the bundled script. It reads the package in the local NuGet cache, so it needs no network access:

```bash
bash .claude/skills/mudblazor-ui/scripts/mud-api.sh params MudButton      # parameters, including inherited ones
bash .claude/skills/mudblazor-ui/scripts/mud-api.sh enum StretchItems     # enum values
bash .claude/skills/mudblazor-ui/scripts/mud-api.sh css justify-start     # does this utility class exist? what does it do?
bash .claude/skills/mudblazor-ui/scripts/mud-api.sh icon TrackChanges     # does Icons.Material.*.TrackChanges exist?
```

Some enums (`Severity`, `MaxWidth`, `InputType`) have no XML docs, so `enum` prints nothing for them even though they exist. In that case trust the build: run the `build-check` skill.

## Mapping: what to use instead of HTML

| Need | Use |
|---|---|
| Vertical/horizontal layout, gaps | `MudStack` (`Row`, `Spacing="0..16"`, `AlignItems`, `Justify`, `Wrap="Wrap.Wrap"`, `StretchItems`). Prefer it over margins. |
| Semantic section/header | `MudStack HtmlTag="section"` / `"header"` (MudStack has `HtmlTag`; **MudPaper does not**) |
| Page width | `MudContainer MaxWidth="MaxWidth.Small"` (600px; pages) or `ExtraSmall` (narrow, centered screens). `Gutters="false"` when the child pads itself |
| Any text | `MudText Typo=... Color=...`; page title = `Typo.h5 HtmlTag="h1"` (`FocusOnNavigate` focuses the `h1`) |
| Muted/helper text | `Color="Color.Secondary"` (Secondary is the slate gray in `AppTheme`, not a brand accent) |
| Ink text inside a colored parent | `Color="Color.Dark"`. `Color.Default` adds no class and **inherits**, so inside a primary button it turns blue |
| Two-column grid | `MudGrid Spacing` + `MudItem xs="6"` (only when a real grid is needed) |
| Buttons | `MudButton Variant Color Size FullWidth StartIcon Disabled OnClick`; icon-only → `MudIconButton` with `aria-label` |
| Text input | `MudTextField T="string" @bind-Value Immediate="true" Variant="Variant.Outlined" InputType InputMode MaxLength Placeholder`; validation → `Error` + `ErrorText`. Plain lowercase HTML attributes (`autocomplete`, `aria-label`) splat through |
| Card | `MudCard Outlined` + `MudCardHeader` (`CardHeaderAvatar`, `CardHeaderContent`) + `MudCardContent` + `MudCardActions` |
| Pill / tag | `MudChip T="string"` (needs `T`; works without `MudChipSet`). Has `Icon` (start) and `OnClick`, **no `EndIcon`**. Add `Class="ma-0"` inside a MudStack |
| Notice / banner | `MudAlert Severity Variant="Variant.Text" Dense Icon`; neutral gray = `Severity.Normal`, brand blue = `Severity.Info` |
| Spinner | `MudProgressCircular Indeterminate="true" Color="Color.Primary"` |
| Progress bar | `MudProgressLinear Value="0..100" Rounded="true" Size` |
| Placeholder bars | `MudSkeleton Width="55%" Animation="Animation.False"` |
| Divider with a label | Row `MudStack`: `MudDivider Class="flex-1"` / `MudText Typo.caption` / `MudDivider Class="flex-1"` (no built-in) |
| Icons | Material icons for UI chrome (`Icons.Material.Filled.*`); emoji only when they come from data (`ReferenceData` choice icons) |
| Logo | `BrandMark` (shared component), never an inline `<svg>` |

Useful built-in utility classes (verified): `pa-*`/`px-*`/`py-*`/`ma-0`, `flex-1`, `flex-grow-1`, `flex-shrink-0`, `flex-nowrap`, `overflow-x-auto` (sideways-scrolling chip/tile rows), `overflow-hidden`, `align-self-start`/`-center`/`-end`, `justify-start`, `mud-width-full`, `mud-height-full`, `rounded-pill`, `rounded-lg`, `rounded-circle`, `rounded-t-xl`, `border-2`, `border-t`, `border-solid`, `mud-border-primary`, `mud-border-lines-default`, `mud-background-gray`, `mud-primary-hover` (primary tint background), `mud-theme-primary` (primary background + contrast text), `mud-elevation-2`, `object-cover`, `relative`, `absolute`, `sticky`.
**These do NOT exist:** `w-100`, `min-vh-100`, `top-0`, `bottom-0`, `text-left`, `text-start`, `text-truncate` (use the `one-line` gap), `font-weight-bold`, `position-sticky`, `mud-theme-surface`. Check others with `mud-api.sh css`.

## Workarounds (only when MudBlazor can't)

1. Look for a parameter first, then a utility class (`mud-api.sh css`).
2. If neither exists, add a small, named class to the **"MudBlazor gaps"** section of `SideGigPh.Maui/wwwroot/app.css` and use it via `Class=`. Current helpers: `full-screen` (100vh), `sticky-footer` (interview Next), `choice-tile`, `brand-logo`, `own-listing`, `work-thumb` (square photo), `photo-remove`, `profile-photo` (6rem avatar), `one-line` (ellipsis), and the app shell's `app-top-bar`, `app-bottom-nav`, `app-content`, `app-fab`, `app-sticky-action` (a page's main action pinned just above the bottom tabs; put it last in the page). Reuse one before adding another.
3. **Never add a `.razor.css` file.** Scoped CSS only tags elements written directly in that file, and here every element is rendered by a MudBlazor component, so scoped rules never match.
4. CSS colors come only from theme variables: `var(--mud-palette-primary)`, `rgba(var(--mud-palette-primary-rgb), .08)`, `var(--mud-palette-lines-default)`, `var(--mud-palette-background-gray)`. No hex.

## Theme

`SideGigPh.Maui/Components/Layout/AppTheme.cs` (a `MudTheme`) is the single source of truth for palette, typography and radius; `MainLayout` passes it to `MudThemeProvider`. To restyle, edit only that file. Primary `#0078d4`, Info = brand blue, Secondary = muted slate, Dark = navy ink, buttons `TextTransform = "none"`, radius 10px. Typography types in v9 are `DefaultTypography`, `H5Typography`, `ButtonTypography`… with string `FontWeight`. If you change the primary or background color, also update the splash fallbacks in `app.css` (`var(--mud-palette-primary, #0078d4)`), because the splash paints before the theme exists.

## Component rules that interact with MudBlazor

- Code-behind is mandatory, so there are **no `@code` blocks**, including `RenderFragment` helpers. If you need a reusable chunk of markup, make it a small component (`ChoiceTile`, `ProCardFact` are examples).
- A parent that needs to style a child component exposes a `[Parameter] public string? Class` and passes it to the root Mud component (see `ProCard`).
- User-facing text still comes from `Copy.cs` through `AppText T`; MudBlazor doesn't change the bilingual rule.

## Gotchas seen in this repo

| Symptom | Cause | Fix |
|---|---|---|
| `CS0103: The name 'search' does not exist` on a lowercase attribute like `inputmode="search"` | Attribute names match parameters **case-insensitively**, so `inputmode` binds to `MudTextField.InputMode` (an enum) and the value is parsed as C# | Use the parameter: `InputMode="InputMode.search"`. Only attributes with no matching parameter (`aria-label`, `autocomplete`, `enterkeyhint`) splat through |
| `CS0104: 'Size' is an ambiguous reference` in a `.razor.cs` | MAUI's global usings bring `Microsoft.Maui.Graphics.Size` alongside `MudBlazor.Size` | `using Size = MudBlazor.Size;` (see `PersonAvatar.razor.cs`) |
| Text inside a whole-card link turns brand blue | `MudLink` defaults to `Color.Primary` and `MudText` inherits | `MudLink Underline="Underline.None" Color="Color.Inherit"` (see `ResultCard`, `ThreadRow`) |
| `MudAppBar Color="Color.Surface"` / `MudPaper HtmlTag` | No `mud-theme-surface` class (transparent bar); MudPaper has no `HtmlTag` | White fixed bar = `MudPaper Square Elevation` + a gap class; landmarks via `role="navigation"`. `MudPaper` does have `MaxWidth`/`MinWidth` |
| `mud-api.sh params MudTextField` shows no `DebounceInterval` | The script skips the `MudDebouncedInput` base class | They exist: `DebounceInterval`, `OnDebounceIntervalElapsed` (see `SearchBox`) |

## Before you finish

- Grep the files you changed for raw tags: `grep -nE '<(div|span|p|h[1-6]|button|input|label|section|ul|li|svg|a)[ >/]' -r SideGigPh.Maui/Components --include=*.razor` (should print nothing).
- Grep for hex colors or `--color-` in the files you changed.
- Run the `build-check` skill to compile. It catches wrong parameter and enum names.
- If you added a shared component, a gap class or a theme change, update `CLAUDE.md`.
