---
name: new-component
description: Add a new Razor component or page/screen to SideGigPh the way this repo requires — correct folder, .razor + .razor.cs code-behind, bilingual English/Taglish copy in Copy.cs, MudBlazor-only markup, and CLAUDE.md upkeep. Use when asked to add, build, create or split out a component, page, screen, step, card, section or route, or when a .razor file is getting too big and needs extracting.
---

# Adding a component or page

Work through these in order. Each step points to the repo's existing pattern.

## 1. Decide where it lives

- **Feature component**: used by one feature. Put it in `SideGigPh.Maui/Components/<Feature>/` (`Onboarding`, `Search`, `Professional`, `Auth`…). Namespace = `SideGigPh.Maui.Components.<Feature>`.
- **Generic component**: used by two or more features and tied to none. Put it in `SideGigPh.Maui/Components/Shared/` (namespace `…Components.Shared`, already imported everywhere). Examples: `InterviewStep`, `ChoiceTiles`/`ChoiceTile`, `ProCard`/`ProCardFact`, `GoalPayoff`, `WorkingLoader`, `BrandMark`.
- **Layout or theme**: `Components/Layout/`.
- A new feature folder whose components are used from another namespace needs `@using SideGigPh.Maui.Components.<Feature>` in `Components/_Imports.razor`.
- Before writing a new one, check `Components/Shared/`: a card, tile, step shell or loader may already exist.

## 2. Two files, always

`<Name>.razor` holds markup only. `<Name>.razor.cs` holds a `public partial class <Name> : ComponentBase` with all logic. **No `@code` blocks**, not even for a `RenderFragment` helper; extract a small component instead.

```razor
@* Components/Search/SavedSearchCard.razor *@
<MudCard Outlined="true">
    <MudCardContent>
        <MudStack Spacing="2">
            <MudText Typo="Typo.h6" HtmlTag="h2">@Title</MudText>
            <MudText Typo="Typo.body2" Color="Color.Secondary">@T.Get(Copy.SavedSearchHint)</MudText>
            <MudButton Variant="Variant.Filled" Color="Color.Primary" FullWidth="true"
                       StartIcon="@Icons.Material.Filled.Search" OnClick="OnOpen">
                @T.Get(Copy.SavedSearchOpen)
            </MudButton>
        </MudStack>
    </MudCardContent>
</MudCard>
```

```csharp
// Components/Search/SavedSearchCard.razor.cs
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

Conventions seen across the repo:
- Inputs are `[Parameter]` (with `EditorRequired` when mandatory); outputs are `EventCallback<T>`. A component reports what happened, and the parent decides what it means (see `ChoiceTiles.OnPick`).
- Services come from `[Inject]` properties: `AppText T`, `NavigationManager Nav`, `IOnboardingStore Store`, ports like `IProfessionalProfileRepository`. Never a concrete adapter.
- A child that the parent must style gets `[Parameter] public string? Class` passed to its root Mud component.
- Private state uses `_camelCase` fields; derived values are expression-bodied properties.

## 3. Pages and routes

- `@page "/route"` goes at the top of the `.razor` file. Interview-style flows get **one URL per step** (`/start/pro/{Step:int?}`), so Android back walks the steps. Save answers before you navigate forward.
- Each page has exactly one `MudText … HtmlTag="h1"` (the title). `FocusOnNavigate` focuses it.
- Wrap the page in `MudContainer MaxWidth="MaxWidth.Small"` with a `MudStack` inside. Use `InterviewStep` for any question screen.
- Don't add login or auth gates to the onboarding flow. The owner is still designing that.

## 4. Every user-facing string is bilingual

Add pairs to `SideGigPh.Maui/Services/Localization/Copy.cs` under the matching `// --- Section ---` comment:

```csharp
public static readonly LocalizedText SavedSearchHint = new("Open your last search", "Buksan ang huling hinanap mo");
// {0} = town label
public static readonly LocalizedText SavedSearchTown = new("Pros in {0}", "Mga pro sa {0}");
```

Render with `@T.Get(Copy.X)` or `@T.Format(Copy.X, arg)`. Answer lists (jobs, towns, price tiers) belong in `ReferenceData.cs`, not `Copy.cs`.

Write for users who are not confident English readers: short plain words, numbers and ₱ doing the work, and an icon on every choice tile. Taglish is everyday conversational, not formal Filipino. Keep English trade terms (aircon, chemical wash, breaker) and use words people actually say (CR, gripo, lababo, kisame). If you're unsure of the Taglish, write your best version and tell the user it needs a native check.

## 5. Markup is MudBlazor only

Follow the `mudblazor-ui` skill: MudStack for layout and spacing, MudText for text, theme `Color`s only, no raw HTML, no `.razor.css`, gaps go in `app.css`. Verify unfamiliar parameters with its `mud-api.sh` script.

## 6. Keep the docs true

- New **generic** component: add it to the Shared list in `CLAUDE.md` (Coding Standards → "Generic vs feature components").
- New route, flow step, port or service: update `CLAUDE.md` and, for onboarding, `docs/onboarding.md`.
- New `app.css` gap class or theme change: covered in the `mudblazor-ui` skill's checklist.

## 7. Verify

- Run the `build-check` skill. It must end in `BUILD OK`.
- Check that no raw HTML tags or hard-coded English strings slipped into the new `.razor` files.
- Tell the user what still needs a look on a device, since you can't see it render.
