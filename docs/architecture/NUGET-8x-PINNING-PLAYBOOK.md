# NuGet 8.x Pinning Playbook – EMMA Architecture

Version: 1.0
Last Updated: 2025-09-13
Status: ACTIVE – Architecture Guardrail

Note: This playbook is part of the EMMA Architecture Guardrails (see `docs/architecture/ARCHITECTURE.md`) and governs all NuGet usage across the platform.

## Overview

- Purpose: Keep the EMMA platform stable and Azure AI Foundry–compatible by pinning NuGet to 8.x and enforcing Central Package Version Management (CPVM) with lock files.
- Scope: Applies to local dev, CI, and release pipelines. No promotion to NuGet 9.x is allowed.
- Goals:
  - Deterministic restores with per-project lock files.
  - Single source of truth for package versions via CPVM.
  - CI guardrails to prevent accidental drift or NuGet 9.x usage.

## File Structure

- Split CPVM strategy:
  - Directory file for evaluation and lockfile flags:
    - Path: `Directory.Build.props`
    - Responsibility: Turn on CPVM and lockfile enforcement for all projects.
  - Directory file for package pins:
    - Path: `Directory.Packages.props`
    - Responsibility: Declare all `<PackageVersion>` entries used across the solution.
- Typical contents:
  - `Directory.Build.props` (flags only)

    ```xml
    <Project>
      <!-- Central Package Version Management -->
      <PropertyGroup>
        <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>

        <!-- Lock files on by default; restores fail if lock and graph diverge -->
        <RestorePackagesWithLockFile>true</RestorePackagesWithLockFile>
        <RestoreLockedMode>true</RestoreLockedMode>
      </PropertyGroup>
    </Project>
    ```

  - `Directory.Packages.props` (pins only)

    ```xml
    <Project>
      <ItemGroup>
        <!-- Example: central pins for all projects -->
        <PackageVersion Include="Newtonsoft.Json" Version="13.0.3" />
        <PackageVersion Include="Serilog" Version="3.1.1" />
        <PackageVersion Include="Microsoft.Extensions.Configuration" Version="8.0.0" />
        <PackageVersion Include="Microsoft.Extensions.Configuration.EnvironmentVariables" Version="8.0.0" />
        <!-- Add all other packages here; no per-.csproj versions -->
      </ItemGroup>
    </Project>
    ```

- Lock files:
  - Per-project `packages.lock.json` are generated and must be committed.
  - Restores run in `--locked-mode` to ensure determinism.

## Workflow

Sequence: Verify → Detect → Remediate → Re-lock

### Verify

- Confirm CPVM is on and lock mode is enforced.
- `Directory.Build.props` includes:
  - `ManagePackageVersionsCentrally=true`
  - `RestorePackagesWithLockFile=true`
  - `RestoreLockedMode=true`
- Confirm NuGet client is 8.x.
  - `nuget help` (should report 8.x)
  - In GitHub Actions, ensure `NuGet/setup-nuget` is set to `8.x`.

### Detect

- Find per-project version drift (should be none with CPVM).
  - `dotnet build` should not show per-project version overrides.
- Identify outdated packages (optional, for planned upgrades only).
  - `dotnet list path/to.csproj package --outdated`
- Confirm no lockfile drift.
  - `dotnet restore --locked-mode` must succeed.

### Remediate

- All version changes must happen in `Directory.Packages.props`.
- Do not change versions in individual `.csproj` files.
- If a restore fails due to graph drift:
  - Update the relevant `<PackageVersion>` in `Directory.Packages.props`.
  - Run a non-locked restore once to refresh lock files.
    - `dotnet restore --use-lock-file`
- Rebuild to validate.
  - `dotnet build -warnaserror`

### Re-lock

- Confirm lock files match the central pins.
  - `dotnet restore --locked-mode`
- Commit updated `packages.lock.json`.
  - `git add **/packages.lock.json`
  - `git commit -m "chore(deps): re-lock after central pin updates"`

## CI Guardrail

- GitHub Actions workflow example: `.github/workflows/nuget-guard.yml`
  - Enforces:
    - NuGet 8.x client.
    - Locked-mode restore.
    - Fails fast on NuGet 9.x.
    - Verifies CPVM flags present.
  - Example:

    ```yaml
    name: NuGet Guard

    on:
      pull_request:
      push:
        branches: [ main ]

    jobs:
      guard:
        runs-on: ubuntu-latest

        steps:
          - name: Checkout
            uses: actions/checkout@v4

          - name: Setup .NET SDK
            uses: actions/setup-dotnet@v4
            with:
              dotnet-version: '8.0.x'

          - name: Setup NuGet 8.x
            uses: NuGet/setup-nuget@v2
            with:
              nuget-version: '8.x'  # strictly 8.x

          - name: Verify NuGet is 8.x (fail on 9.x)
            shell: bash
            run: |
              VER=$(nuget help | sed -n 's/.*NuGet Version: \([0-9.]*\).*/\1/p')
              echo "Detected NuGet: $VER"
              if [[ "$VER" == 9.* ]]; then
                echo "NuGet 9.x detected. Failing per EMMA policy."
                exit 1
              fi

          - name: Validate CPVM flags exist
            shell: bash
            run: |
              if ! grep -q "<ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>" Directory.Build.props; then
                echo "CPVM flag missing in Directory.Build.props"
                exit 1
              fi
              if ! grep -q "<RestorePackagesWithLockFile>true</RestorePackagesWithLockFile>" Directory.Build.props; then
                echo "Lockfile flag missing in Directory.Build.props"
                exit 1
              fi
              if ! grep -q "<RestoreLockedMode>true</RestoreLockedMode>" Directory.Build.props; then
                echo "Locked mode flag missing in Directory.Build.props"
                exit 1
              fi

          - name: Restore (locked mode)
            run: dotnet restore --locked-mode

          - name: Build (fail on warnings)
            run: dotnet build -warnaserror
    ```

- Cross-platform note: The simple `grep` shell checks above run on Linux/macOS runners. On Windows runners or local Windows shells, use the PowerShell equivalents shown in the Appendix.

## Quick Fix Checklist

### 10-Minute Remediation Flow for Devs

1. Pull latest `main` and ensure clean working tree.

1. Confirm NuGet 8.x locally.

  - `nuget help` → must show 8.x.

1. If restore fails in `--locked-mode`.

  - Open `Directory.Packages.props`.
  - Adjust the relevant `<PackageVersion Include="..." Version="..."/>`.

1. Re-lock.

  - `dotnet restore --use-lock-file` (updates lock files)
  - `dotnet restore --locked-mode` (sanity check)

1. Build to verify.

  - `dotnet build -warnaserror`

1. Commit lockfiles and props update.

  - `git add Directory.Packages.props **/packages.lock.json`
  - `git commit -m "chore(deps): central pin + re-lock"`

1. Push and open PR; CI must pass NuGet Guard.

## Weekly Hygiene

- Review outdated packages:
  - Run `dotnet list **/*.csproj package --outdated` (or per project) to plan minor/patch bumps.
- Batch upgrades centrally:
  - Edit `Directory.Packages.props` only; never per-project.
- Re-lock and verify:
  - `dotnet restore --use-lock-file` then `--locked-mode`.
- Observe CI:
  - Keep NuGet Guard green. Investigate any NuGet 9.x attempts immediately.
- Audit drift:
  - Search weekly for accidental per-project versions:
    - `grep -R "<PackageReference[^>]*Version=" -n -- */*.csproj` (Linux/macOS)
    - PowerShell version in Appendix (Windows)

## Rollback Policy

- Never roll forward to NuGet 9.x. If any developer or pipeline picks up 9.x:
  - Immediate action:
    - Pin NuGet back to 8.x (local: reinstall or use `NuGet/setup-nuget@v2` with `8.x`).
    - Re-run `dotnet restore --locked-mode`.
  - If lock files changed under 9.x:
    - Discard those changes.
    - Re-run `dotnet restore --use-lock-file` under 8.x to regenerate.
- Temporary exceptions (rare and pre-approved):
  - Must be approved by Architecture owners and Security.
  - Scoped to a dedicated branch.
  - CI must still run with NuGet 8.x; exceptions cannot merge to `main`.
  - Document the rationale and expiration date in the PR description.
- Incident tracking:
  - Any violation (NuGet 9.x usage, per-project version pins, missing lock files) gets a Sev-3 ticket with owner and ETA.

## Appendix

- XML snippets
  - `Directory.Build.props`

    ```xml
    <Project>
      <PropertyGroup>
        <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
        <RestorePackagesWithLockFile>true</RestorePackagesWithLockFile>
        <RestoreLockedMode>true</RestoreLockedMode>
      </PropertyGroup>
    </Project>
    ```

  - `Directory.Packages.props`

    ```xml
    <Project>
      <ItemGroup>
        <!-- Centralized pins -->
        <PackageVersion Include="Microsoft.Extensions.Configuration" Version="8.0.0" />
        <PackageVersion Include="Microsoft.Extensions.Configuration.EnvironmentVariables" Version="8.0.0" />
        <!-- Add/maintain all other pins here -->
      </ItemGroup>
    </Project>
    ```

- YAML snippet (NuGet Guard CI)

  ```yaml
  name: NuGet Guard
  on: [push, pull_request]
  jobs:
    guard:
      runs-on: ubuntu-latest
      steps:
        - uses: actions/checkout@v4
        - uses: actions/setup-dotnet@v4
          with:
            dotnet-version: '8.0.x'
        - uses: NuGet/setup-nuget@v2
          with:
            nuget-version: '8.x'
        - name: Verify NuGet 8.x
          run: |
            VER=$(nuget help | sed -n 's/.*NuGet Version: \([0-9.]*\).*/\1/p')
            echo "NuGet: $VER"
            if [[ "$VER" == 9.* ]]; then exit 1; fi
        - name: Restore (locked)
          run: dotnet restore --locked-mode
        - name: Build (fail on warnings)
          run: dotnet build -warnaserror
  ```

- CLI commands
  - Verify NuGet version:
    - `nuget help` (look for `NuGet Version: 8.x`)
  - Locked restore:
    - `dotnet restore --locked-mode`
  - Re-lock after changing central pins:
    - `dotnet restore --use-lock-file`
  - Show outdated packages (for planning):
    - `dotnet list path/to.csproj package --outdated`
  - Find accidental per-project version pins:
    - Linux/macOS:

      ```bash
      grep -R "<PackageReference[^>]*Version=" -n -- */*.csproj
      ```

    - PowerShell (Windows):

      ```powershell
      Get-ChildItem -Recurse -Filter *.csproj |
        Select-String -Pattern '<PackageReference[^>]*Version=' |
        ForEach-Object { "$($_.Filename):$($_.LineNumber) $($_.Line)" }
      ```

## Acceptance Criteria

- This document is self-contained, readable by new developers in under 15 minutes, and serves as the governing policy for all NuGet usage in EMMA.
- It aligns with the Architecture Guardrails and maintains the split CPVM strategy:
  - Flags in `Directory.Build.props`
  - All pins in `Directory.Packages.props`
- It explicitly prohibits NuGet 9.x and provides cross-platform guidance for CI and local development.
