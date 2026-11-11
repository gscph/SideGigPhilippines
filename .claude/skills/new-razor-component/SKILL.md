---
name: new-razor-component
description: Add, split out or restyle a Razor component, page, screen, tab, card, section or route in SideGigPh the house way — feature folder vs Components/Shared, .razor + .razor.cs code-behind, MudBlazor-only markup, AppTheme colors, bilingual Copy.cs text, ports injected via DI, pages inside the shared AppShell (ProLayout / SeekerLayout, ProRoutes / SeekerRoutes), and CLAUDE.md upkeep. Use whenever adding a .razor file, a new page/route/tab, extracting part of a page that got too big, or changing a component's layout or look. Includes ui-lint.sh to check .razor files for raw HTML, hex colors, literal text and missing h1s.
---

# New Razor component or page (SideGigPh house rules)

Work through these in order. Each step points to the repo's existing pattern.

## 1. Decide where it goes

- **Feature component** (used by one feature): `SideGigPh.Maui/Components/<Feature>/` — `Onboarding`, `Professional`, `Seeker`, `Search`, `Inbox`, `Auth`. Namespace = `SideGigPh.Maui.Components.<Feature>`. A new feature folder needs its `@using` in `Components/_Imports.razor`.
- **Skeleton component** (any product would use it unchanged, no SideGigPh words): `MobileAppTheme/Components/Shared/` (namespace `MobileAppTheme.Components.Shared`, imported everywhere). **Load the `mobile-app-theme` skill first**: it has the rules (no app references, `CommonCopy` not `Copy`, domain data via parameters) and `skeleton-check.sh`. When unsure, start in the app and promote later.
- **App-generic component** (used by two or more SideGigPh features, tied to its domain): `SideGigPh.Maui/Components/Shared/` (namespace `SideGigPh.Maui.Components.Shared`). The current lists are in `CLAUDE.md` (Coding Standards → Skeleton vs app vs feature components). **Reuse before creating**: the pro preview and search results both use `ProCard`; both homes use the skeleton's `HomeSection`; the card and the public profile both use `ListingFacts` + `ServiceChips`.
- **App frame or layout**: the skeleton's `MobileAppTheme/Components/Layout/` (`MainLayout`, `AppShell`, `AppTopBar`, `AppBottomNav`, `AppTab`). SideGigPh's brand (`AppTheme.cs`) and its side layouts (`ProLayout`, `SeekerLayout`) stay in the app.
- **Keep components small and single-purpose.** Inputs are `[Parameter]`s, outputs are `EventCallback`s. Never one giant page; extract a repeated or self-contained piece.

## 2. Always two files (code-behind)

`<Name>.razor` holds markup only. `<Name>.razor.cs` holds `public partial class <Name> : ComponentBase`. **No `@code` blocks**, not even a `RenderFragment` helper; extract a small component instead.

```razor
@* Components/Search/SavedSearchCard.razor *@
<MudCard Outlined="true">
    <MudCardContent>
        <MudStack Spacing="2">
            <MudText Typo="Typo.h6" HtmlTag="h2">@Title</MudText>
            <MudText Typo="Typo.body2" Color="Color.Secondary">@T.Get(Copy.SavedSearchHint)</MudText>
            <MudButton Variant="Variant.Filled" Color="Color.Primary" FullWidth="true"
                       StartIcon="@Icons.Material.Filled.Search" OnClick="@(() => OnOpen.InvokeAsync())">
                @T.Get(Copy.SavedSearchOpen)
            </MudButton>
        </MudStack>
    </MudCardContent>
</MudCard>
```

```csharp
// Components/Search/SavedSearchCard.razor.cs
// AppText, CommonCopy and the other skeleton types come from GlobalUsings.cs; Copy needs its using.
using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Search;

/// <summary>One line on what it is and why it exists.</summary>
public partial class SavedSearchCard : ComponentBase
{
    [Inject] private AppText T { get; set; } = default!;

    [Parameter, EditorRequired] public string Title { get; set; } = string.Empty;

    [Parameter] public EventCallback OnOpen { get; set; }
}
```

Conventions:
- `EditorRequired` on mandatory parameters. A component reports what happened; the parent decides what it means (`ChoiceTiles.OnPick`).
- `[Inject]` **ports and app services only** (`AppText`, `NavigationManager`, `IOnboardingStore`, `IContactLauncher`, `ProWorkspace`, `SeekerWorkspace`…), never an adapter, Firebase or MAUI type. New infrastructure: see the `ports-and-stubs` skill.
- A child the parent must style gets `[Parameter] public string? Class` passed to its root Mud component.
- Private state is `_camelCase`; derived values are expression-bodied properties. Repeated label logic goes in a helper (`Services/Localization/ListingText`), not copied between components.

## 3. Pages, routes and the app shell

- `@page "/route"` at the top of the `.razor`. Exactly one `MudText … HtmlTag="h1"` per page (`FocusOnNavigate` focuses it).
- **Interview / onboarding pages**: `InterviewStep` inside `MudContainer MaxWidth.Small`, one URL per step (`/start/pro/{Step:int?}`), save before navigating forward (see `add-interview-step`). No auth gates in the flow; the owner is still designing that.
- **Signed-in pages** sit inside the shared `AppShell` (brand bar + avatar, content, bottom tabs) via `@layout ProLayout` or `@layout SeekerLayout`. Don't add your own `MudContainer`; the shell provides it.
  - Add the route to `ProRoutes` / `SeekerRoutes` and link through it (`Href="@SeekerRoutes.Thread(id)"`), never a literal path.
  - A new **tab** is an `AppTab` in that layout's `Tabs` (icon, label from `Copy`, optional `Badge`, `MatchPrefix: true` if it has sub-pages like `/inbox/{id}`). Keep 3–4 tabs; put a sub-page under its tab's route (`/me/reviews`, `/seeker/search`) with `MatchPrefix: true` so the tab stays lit.
  - The page's main action (Contact, Send) goes **last** in the page inside `MudPaper Elevation="0" Square="true" Class="app-sticky-action py-2"` so it stays above the tabs.
  - Shared state lives in the side's singleton workspace (`ProWorkspace`, `SeekerWorkspace`) with a `Changed` event; layouts subscribe and unsubscribe in `Dispose`. Pages call the workspace, not the repositories.
- **Focused full-screen steps** after the interview (publish, write a review) use `InterviewStep` with **no** `@layout`, and call `Workspace.EnsureLoadedAsync()` themselves (send to `/` when it returns false).
- Route parameters: `@page "/pro/{Id}"` + `[Parameter] public string Id`. Load what depends on it in `OnParametersSet(Async)`: the same instance is reused when only the parameter changes.

## 4. Markup: MudBlazor only

Follow the `mudblazor-ui` skill: `MudStack` for layout, `MudText` for text, theme `Color`s only (`Primary` brand blue, `Secondary` helper slate, `Dark` ink, `Success` "fits budget" green), no raw HTML, no `.razor.css`, gaps in `app.css`, verify unfamiliar parameters with its `mud-api.sh`. Mobile-first: big tap targets (`Size.Large`, `FullWidth="true"`) for answers and primary actions.

## 5. Every user-facing string is bilingual

Add `LocalizedText` pairs to `Services/Localization/Copy.cs` under the matching `// --- Section ---` comment; render with `@T.Get(Copy.X)` / `@T.Format(Copy.X, arg)`. Generic words the skeleton already has (Back, Next, Cancel, Start over, Language, relative times) come from `CommonCopy`; don't duplicate them in `Copy`. Skeleton components use `CommonCopy` only. Answer lists belong in `ReferenceData.cs`. Follow the `taglish-copy` skill (plain words, numbers and ₱ doing the work, everyday Taglish) and run its `copy-check.sh`.

## 6. Razor and C# gotchas seen in this repo

- Non-string parameters take C# expressions (`Items="Options"`); wrap operators: `CanContinue="@(Draft.CityIds.Count > 0)"`.
- Collection expressions in conditionals: `var x = cond ? [] : list;` doesn't compile. Declare the type (`IReadOnlyList<CatalogOption> x = …`) or use `Enumerable.Empty<T>()` / `Array.Empty<T>()`.
- `using Size = MudBlazor.Size;` in a `.razor.cs` that uses `Size` (MAUI has one too). `using MudBlazor;` in a `.razor.cs` that uses `Icons`.
- Avoid member names that shadow namespaces or MAUI types (`Services`, `Contact`, `Layout`, `Search`). Global usings include `Microsoft.Maui.Controls` and `Microsoft.Maui.ApplicationModel.Communication`.
- A lowercase attribute that matches a parameter name binds to the parameter (`inputmode` → `InputMode`); see `mudblazor-ui` gotchas.
- Async results that can arrive out of order (search-as-you-type) need a version counter so an older result never overwrites a newer one (`SearchPage.RunSearchAsync`).
- Cancel timers and delays in `Dispose` (`WorkingLoader`).

## 7. Finish

- **No dead code.** Delete what's replaced (components, Copy keys, gap classes); don't comment it out.
- Run the checks:
  ```bash
  bash .claude/skills/new-razor-component/scripts/ui-lint.sh          # raw HTML, hex, literal text, h1 per page
  bash .claude/skills/taglish-copy/scripts/copy-check.sh              # Copy keys missing / unused
  bash .claude/skills/mobile-app-theme/scripts/skeleton-check.sh      # when you touched MobileAppTheme/ or CommonCopy
  ```
- Compile with the `build-check` skill (a throwaway copy). **Never build or deploy in the repo folder from the CLI**: the user deploys from Visual Studio, and concurrent builds cause locks and "emulator not ready".
- Update `CLAUDE.md` for a new generic component, route, tab, port or pattern (and `docs/onboarding.md` for onboarding routes).
- Tell the user what still needs a look on a device: you can't see it render.
