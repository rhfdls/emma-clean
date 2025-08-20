# AI‑assisted contribution guide

Purpose: Central entry point for using Windsurf Cascade with this repo.

## Quick links

- Assistant brief (authoritative rules): `.windsurf/assistant-brief.md`
- Contributing guide for AI work: `.windsurf/CONTRIBUTING-AI.md`

## How to use

1) Start tasks by pasting a template from `.windsurf/CONTRIBUTING-AI.md` section 2 or 6.
2) Keep requests structured with blocks like `<task>`, `<constraints>`, `<acceptance>`, `<run>`.
3) Expect proactive, multi‑file diffs; explanations will be brief, diffs verbose.

## Must‑follow rules (summary)

- Central Package Management only; edit `Directory.Packages.props`; locked restore.
- No edits under `archive/`.
- AI calls go through orchestrator/services; thin controllers; services in `src/Emma.Core/Services/`.
- Respect tenant/subscription checks; validation → override → audit.

Full details in `.windsurf/assistant-brief.md`.

## Validate locally

```powershell
# From repo root
 dotnet restore --locked-mode
 dotnet build /bl:msbuild.binlog
 dotnet test -c Release
```

## Example prompt (ready to paste)

```
<task>Implement {change} touching {paths}</task>
<constraints>CPM only; locked restore; preserve logging; no secrets; no archive/</constraints>
<acceptance>{tests}; CI green; validation/override unchanged</acceptance>
<run>dotnet restore --locked-mode && dotnet build && dotnet test</run>
```
