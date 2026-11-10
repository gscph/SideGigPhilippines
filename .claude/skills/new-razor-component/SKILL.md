---
name: new-razor-component
description: Create or restyle a Blazor component or page in SideGigPh the house way — MudBlazor-only markup, code-behind partial class, feature folder vs Components/Shared, AppTheme colors, bilingual text, ports injected via DI. Use whenever adding a .razor file, a new page/route, or changing a component's layout or look.
---

# New Razor component (SideGigPh house rules)

## 1. Decide where it goes

- **A feature component** goes in `SideGigPh.Maui/Components/<Feature>/`: Onboarding, Search, Professional, Auth.
- **A generic component** is used by two or more features and isn't tied to one. It goes in `Components/Shared/` (namespace `SideGigPh.Maui.Components.Shared`), until a `SideGigPh.Shared` project exists. Existing ones: `InterviewStep`, `ChoiceTiles`/`ChoiceTile`, `WorkingLoader`, `ProCard`/`ProCardFact`, `GoalPayoff`, `BrandMark`. **Reuse before creating.** For example, the pro preview and search results both use `ProCard`.
- **Keep components small and single-purpose.** Inputs are `[Parameter]`s; outputs are `EventCallback`s. Never one giant page. Extract a repeated or self-contained piece into its own component.

## 2. Always two files (code-behind)

`<Name>.razor` holds markup only. `<Name>.razor.cs` holds `public partial class <Name> : ComponentBase` in the folder's namespace.

```csharp
public partial class Example : ComponentBase
{
    [Inject] private AppText T { get; set; } = default!;            // bilingual text
    [Inject] private IOnboardingStore Store { get; set; } = default!; // ports, never adapters
    [Parameter, EditorRequired] public ProfessionalListing Listing { get; set; } = default!;
    [Parameter] public EventCallback<string> OnPick { get; set; }
}
```

Inject **interfaces** (`IOnboardingStore`, `IProfessionalProfileRepository`, `IContactLauncher`, `IIdentityProvider`), never Firebase or MAUI types. New infrastructure gets a port in `Services/` and an adapter registered in `MauiProgram.cs`.

## 3. Markup: MudBlazor only

- **No raw HTML** (`div`, `span`, `p`, `button`, `input`…) in `.razor` files. The only raw HTML is `wwwroot/index.html` (the splash).
- **Layout:** `MudContainer` (page width, usually `MaxWidth.Small`), `MudStack` (`Row`, `Spacing`, `AlignItems`, `Justify`, `Wrap`), `MudGrid`/`MudItem`, `MudPaper`.
- **Text:** `MudText` with `Typo`. Each page has exactly one `HtmlTag="h1"`, because `FocusOnNavigate` focuses it.
- **Controls:** `MudButton`, `MudIconButton`, `MudTextField`, `MudChip`, `MudAlert`, `MudCard`, `MudSkeleton`, `MudProgressLinear`/`MudProgressCircular`, `MudAvatar`, `MudIcon` (`Icons.Material.Filled.*`).
- **Spacing and flex:** MudBlazor utility classes (`pa-4`, `px-2`, `flex-1`, `mud-width-full`, `rounded-pill`, `border-t`…).
- **Gaps:** only when MudBlazor has no parameter or utility, add a small named class to the "MudBlazor gaps" section of `wwwroot/app.css`, using `var(--mud-palette-...)`. Existing ones are `full-screen`, `sticky-footer`, `choice-tile`, `brand-logo`, `own-listing`. **No `.razor.css` files:** scoped CSS doesn't reach inside MudBlazor components.
- **Mobile-first:** it must work at phone width. Use big tap targets (`Size.Large`, `FullWidth="true"`) for answers and primary actions.

## 4. Colors and theme

- Colors come only from MudBlazor `Color` parameters, backed by `Components/Layout/AppTheme.cs`. `Color.Primary` is the brand blue, `Color.Secondary` is the muted helper-text slate, `Color.Dark` is navy ink, and `Color.Success` is the "fits budget" green.
- **Never hard-code hex** in components or CSS. To restyle, change `AppTheme`. The only exception is the splash fallbacks in `app.css`, which must match it.

## 5. Text

Every visible string is `@T.Get(Copy.X)` / `@T.Format(Copy.X, …)` or a `ReferenceData` label. Never literal text (see the `taglish-copy` skill). Answer tiles use `ChoiceItem.From(ReferenceData.X, T)`.

## 6. Razor gotchas seen in this repo

- Component parameters that aren't strings take C# expressions (`Items="Options"`). Wrap anything with operators: `CanContinue="@(Draft.CityIds.Count > 0)"`.
- In a conditional, prefer `Array.Empty<string>()` / `new[] { id }` over `[]` / `[id]`, to avoid collection-expression typing surprises.
- Avoid property names that shadow namespaces or MAUI types (`Services`, `Contact`, `Layout`). The global usings include `Microsoft.Maui.Controls` and `Microsoft.Maui.ApplicationModel.Communication`.
- Optional route parameters: `@page "/start/pro/{Step:int?}"` with `[Parameter] public int? Step`. Load state in `OnInitializedAsync`; compute from parameters in `OnParametersSet` (the same instance is reused when only the parameter changes).
- Cancel timers and delays in `Dispose` (see `WorkingLoader`), so navigation mid-animation doesn't throw.

## 7. Finish

- **No dead code.** Delete what's replaced; don't comment it out.
- If the change alters structure, patterns, theme or design, update `CLAUDE.md` (for example, add a new generic component to the Shared list).
- **Don't run `dotnet build` or deploy from the CLI.** The user builds and deploys from Visual Studio, and concurrent CLI builds cause lock and "emulator not ready" problems. Review by reading, then ask the user to build in VS.
