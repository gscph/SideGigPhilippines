---
name: update-tools
description: Review the current session and create or refine helper scripts in `.claude/skills/*/scripts/`. INVOKE ONLY WHEN THE USER EXPLICITLY ASKS — e.g. "/update-tools", "update the tools", "add that as a tool", "make a script for that". Never trigger this on your own.
---

# Add or improve project tools from this session

This is a **manual, user-invoked** skill. Do not auto-run it. A tool is only worth adding when the user decides a session produced a repeatable check, fix or query — most turns do not, and running this unprompted is noise.

## What earns a tool (and what doesn't)

Create or update a script only for a **repeatable, automatable** task:
- a multi-step check you ran by hand and will run again (lint, search-regression, API verification)
- a repair procedure with fixed steps (clear caches, rewrite a config, restart a service)
- a query that exercises real project code outside the MAUI build
- a guard that catches a class of mistake before commit (like `ui-lint.sh`)

Do **not** make a tool for:
- a one-off investigation, anything `dotnet build` already catches, a trivially short command, or an ephemeral task. If it won't save time on the *next* occurrence, skip it.

## Procedure

1. **Scan the session** for commands, greps, or multi-step checks that were run more than once, took several tries to get right, or would be easy to forget next time.
2. **For each candidate, decide the action — prefer editing over creating:**
   - An existing script already covers the area → **edit** it (add the new case, flag, or mode).
   - Nothing covers it → **create** a new script in the matching skill's `scripts/` folder. If no skill owns that area, consider whether a new skill is needed first (suggest `/update-skills`), or place a standalone script under the closest skill.
   - Already captured well → **no-op**.
3. **Match the house style.** Open an existing script (e.g. `build-check.ps1`, `ui-lint.sh`) and mirror it:
   - **Read-only** — never modify repo source, build output, or project state. Scripts that need a work area take a `workdir` argument (the session scratchpad) and refuse to write inside the repo.
   - **Self-contained** — no dependencies beyond what's in the repo and the .NET SDK. No npm, pip, or extra installs.
   - **Exit code** — 0 on success / clean, non-zero on failure / findings. Print a one-line summary at the end.
   - **Platform** — `.sh` for Bash (Git Bash on Windows), `.ps1` for PowerShell. Prefer `.sh` unless the task needs Windows-specific APIs (drive letters, `subst`, registry). Use `winpath()` when paths must cross the Bash/MSBuild boundary.
   - **Header comment** — what it does, usage line, and that `workdir` must be outside the repo.
4. **Test the script** before saving. Run it once with representative input and confirm it exits cleanly and its output is useful.
5. **Wire it up:**
   - Add a `Script:` line in the parent skill's `SKILL.md` (e.g. `Script: \`ui-lint.sh\``).
   - Update the skill's description if the new script changes its trigger surface.
   - Update `CLAUDE.md`'s "Project Skills" section if the skill entry doesn't already mention the script.
6. **Report** what you did: created / edited / skipped, one line each, so the user can veto.

## Before you finish

- Run each new or changed script once and confirm it exits 0 on a clean repo.
- Confirm you edited an existing script rather than creating a near-duplicate.
- Check that the script never writes inside the repo tree.
- List the changes for the user; don't commit unless asked.
