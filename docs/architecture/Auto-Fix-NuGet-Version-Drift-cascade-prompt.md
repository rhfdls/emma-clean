# 🌊 Windsurf Cascade — Auto-Fix NuGet Version Drift (EMMA, 8.x only)

Version: 1.0
Last Updated: 2025-09-13
Status: ACTIVE – Architecture/Dev Utility

Note: This utility follows the NuGet Governance guardrails. See also: [NUGET-8x-PINNING-PLAYBOOK.md](./NUGET-8x-PINNING-PLAYBOOK.md).

## Context

- Repo: `emma-clean` (.NET 8, Azure-first).
- NuGet policy: 8.x only (no 9.x) with CPVM + lockfiles.
- Split props strategy:
  - `Directory.Build.props` → flags (`ManagePackageVersionsCentrally`, `RestorePackagesWithLockFile`, `RestoreLockedMode`).
  - `Directory.Packages.props` → all `<PackageVersion>` pins.
- Use the existing NuGet 8.x Pinning Playbook as the source of truth.

## Goal

Identify the package(s) causing version drift, pin or correct them centrally, refresh lockfiles deterministically, and produce a clean, compiling build under NuGet 8.x — with a minimal, reviewable diff.

How to run in Windsurf: Paste this prompt into Cascade at the repo root and follow the steps. Attach/collect logs the prompt requests.

---

## Tasks

1. Sanity & environment check

- Confirm NuGet client = 8.x:
  - Run: `nuget help` and capture the version.
  - If `9.*`, do not proceed; add a note to output and exit with failure status.
- Confirm .NET SDK = 8.0.x via `dotnet --info`.

1. Repo scan & fast triage

- From repo root:
  - Run `dotnet restore --locked-mode` and capture full logs.
  - Run `dotnet list package` (solution level) and save output.
- If restore fails or any package resolves to 9.x (direct or transitive), list offenders (name + resolved version + requesting project).

1. Drift detection details

- Parse the logs for `NU1605`, `NU1107`, `NU1010` and include a short explanation per offender.
- Search for inline per-project pins (these are not allowed):
  - Linux/macOS: `grep -R "<PackageReference[^>]*Version=" -n -- */*.csproj`
  - Windows PowerShell:

    ```powershell
    Get-ChildItem -Recurse -Filter *.csproj |
      Select-String -Pattern '<PackageReference[^>]*Version=' |
      ForEach-Object { "$($_.Filename):$($_.LineNumber) $($_.Line)" }
    ```

1. Central remediation (no per-project versions)

- Open `Directory.Packages.props`.
- For each offending package (including transitives), add or correct:

  ```xml
  <PackageVersion Include="PACKAGE_ID" Version="8.x.x" />
  ```

- Remove any inline `<PackageReference Version="...">` in `.csproj` files if found (replace with versionless `<PackageReference Include="..."/>`).

1. Re-lock deterministically

- Refresh and re-lock:
  - `dotnet restore --use-lock-file` (updates `packages.lock.json`)
  - `dotnet restore --locked-mode` (validation)
- Build with warnings as errors: `dotnet build -warnaserror`.

1. Output + minimal diff

- If fixed: produce a concise diff including:
  - `Directory.Packages.props` changes (only new/modified `<PackageVersion>` lines).
  - Any removed `Version=` attributes from `.csproj`.
  - Updated `packages.lock.json` (summarize changed packages/versions; do not dump entire files).
- If not fixed: print the blockers (conflicting constraints, missing 8.x version on a required package, etc.) with the exact log lines.

---

## Constraints

- Do NOT recommend or introduce NuGet 9.x.
- Do not change the split-props architecture.
- All version changes occur only in `Directory.Packages.props`.
- Enforce lockfiles: keep/restore `RestoreLockedMode=true` after graph reconciliation.
- Preserve RFC: fail the task if environment NuGet is 9.x.

---

## Acceptance Criteria

- `dotnet restore --locked-mode` succeeds.
- `dotnet list package` shows no 9.x packages (direct or transitive).
- Build passes: `dotnet build -warnaserror`.
- Diff only includes: central pins, removal of inline versions, and lockfile updates.
- Output includes a one-paragraph root-cause and the exact fix applied.

---

## Deliverables

- Fix summary (offending package(s), cause, remedy).
- Unified diff of edited files:
  - `Directory.Packages.props`
  - Any `.csproj` files that had inline `Version=` removed
  - Brief summary of `packages.lock.json` changes
- Post-fix verification logs (restore/build).

---

## Commands (reference)

Use these during the run (adjust for OS when needed):

```bash
# Verify NuGet 8.x
nuget help

# Restore (locked)
dotnet restore --locked-mode

# List resolved packages
dotnet list package

# Refresh lockfiles after central pin updates
dotnet restore --use-lock-file
dotnet restore --locked-mode

# Build strictly
dotnet build -warnaserror
```

Inline pin removal detection:

- Linux/macOS:

  ```bash
  grep -R "<PackageReference[^>]*Version=" -n -- */*.csproj || true
  ```

- PowerShell (Windows):

  ```powershell
  Get-ChildItem -Recurse -Filter *.csproj |
    Select-String -Pattern '<PackageReference[^>]*Version=' |
    ForEach-Object { "$($_.Filename):$($_.LineNumber) $($_.Line)" }
  ```

---

## Note for Cascade

Follow the NuGet 8.x Pinning Playbook verbatim for semantics and guardrails (CPVM, lockfiles, no per-project versions, no 9.x). Add a short “what changed & why” section in the output so reviewers can approve quickly.
