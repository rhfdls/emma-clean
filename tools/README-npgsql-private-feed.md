# Private Npgsql 8.0.8 Feed

This repo contains scripts to build and host a private, local Npgsql 8.0.8 package to avoid unintended upgrades to 9.x.

## Files
- `tools/build-npgsql-8.0.8.ps1` — clones Npgsql, checks out tag `v8.0.8`, packs `Npgsql.8.0.8.nupkg`, and copies it to `nuget-local-cache/`.
- `tools/verify-local-feed.ps1` — verifies both `Npgsql.8.0.8.nupkg` and `Npgsql.EntityFrameworkCore.PostgreSQL.8.0.8.nupkg` exist and runs a forced restore.

## Prereqs
- Git and .NET 8 SDK on PATH.
- `nuget.config` includes the local feed:
  ```xml
  <configuration>
    <packageSources>
      <add key="LocalPackages" value="nuget-local-cache" />
      <add key="NuGet.org" value="https://api.nuget.org/v3/index.json" />
    </packageSources>
  </configuration>
  ```
- `Directory.Packages.props` pins versions:
  ```xml
  <PackageVersion Include="Npgsql" Version="8.0.8" />
  <PackageVersion Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="8.0.8" />
  ```

## Usage
1) Build and install the private package
```powershell
powershell -ExecutionPolicy Bypass -File .\tools\build-npgsql-8.0.8.ps1
```

2) Verify local feed and restore
```powershell
powershell -ExecutionPolicy Bypass -File .\tools\verify-local-feed.ps1
```

3) Build solution
```powershell
dotnet build
```

## Notes
- The local `.nupkg` is not signature-signed; that is fine for private/internal consumption.
- If restore pulls 9.x, ensure `Directory.Packages.props` pins 8.0.8 and `nuget.config` includes `nuget-local-cache`.
