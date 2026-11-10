---
name: maui-app-branding
description: >
  Create or fix the app launcher icon, native splash screen, and in-app loading screen for a
  .NET MAUI Blazor Hybrid app so it looks like a real app (branded icon + app name + splash).
  Use when branding the app, making a splash/launch screen, or when a generated icon or splash
  renders blank.
---

# MAUI Blazor Hybrid app branding (icon + splash + loading)

## The #1 gotcha: SVG `<text>` does NOT rasterize
MAUI's image resizer (icon/splash → PNG) does **not** reliably render SVG `<text>` elements —
wordmarks come out **blank**. So:
- Build the icon/splash art from **vector paths and primitives only** (`<path>`, `<rect>`,
  `<circle>`, groups with `transform`). No `<text>`.
- Put the **app-name wordmark** in the **HTML loading screen** (`wwwroot/index.html`), which
  renders text perfectly — not in the icon/splash SVG.

## Icon (logo in the tile) + name (label under it)
- Art: `Resources/AppIcon/appicon.svg` (background) + `appiconfg.svg` (foreground mark);
  `<MauiIcon ... Color="#..." />` in the csproj sets the background color.
- The **app name under the icon** on the home screen = `<ApplicationTitle>` in the csproj (the OS
  draws it). So a logo-only icon + `ApplicationTitle` reads like Discord/Grab/Lazada: mark in the
  tile, name beneath — no text baked into the icon.

## Two-stage launch (so the name shows while loading)
1. **Native splash** (`Resources/Splash/splash.svg`, `<MauiSplashScreen Color="..." />`): shows
   instantly on tap — **logo mark only** (no wordmark, per the text gotcha).
2. **HTML loading screen**: the `#app` placeholder in `wwwroot/index.html` — inline-SVG logo +
   the **app-name wordmark** + a spinner, styled with the app's theme tokens/vars. Blazor replaces
   it with the first page once it boots.

Set the native page background (`ContentPage BackgroundColor`) and `body`/`#app` background to the
brand surface so there's no color flash behind the BlazorWebView before the loader paints.

## Verify the generated art isn't blank
After building, inspect the generated PNGs — a blank icon has only one color. On Windows:
```powershell
Add-Type -AssemblyName System.Drawing
$bmp = [System.Drawing.Bitmap]::new("<obj>\...\appiconfg.scale-400.png")
# sample pixels; >1 distinct color == the mark actually rendered
```
Or just open the largest generated tile/splash PNG. If it's a flat square, the SVG didn't
rasterize (usually a stray `<text>` or an unsupported feature) — redo it with paths.

## Colors
Pull every color from the app's design tokens / `MudTheme`, never hard-coded hex — the one
exception is the pre-Blazor splash/loader fallback colors in `index.html` / `app.css`, which
must be literal and kept in sync with the theme.
