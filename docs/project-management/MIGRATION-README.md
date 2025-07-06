# EMMA Database Migration Guide

## How to Use

### ⚠️ EF Core Migration Source of Truth

**Always create and apply EF Core migrations in the `Emma.Data` project.**
- Use `--project src/Emma.Data` for all migration commands.
- Use `--startup-project src/Emma.Api` if your API is the entrypoint.
- Never create migrations in `Emma.Api` or `Emma.Infrastructure`.
- All migration files must appear in `src/Emma.Data/Migrations`.

#### Example Command:
```sh
dotnet ef migrations add <MigrationName> --project src/Emma.Data --startup-project src/Emma.Api
dotnet ef database update --project src/Emma.Data --startup-project src/Emma.Api
```

#### Why?
- Only `Emma.Data` contains the canonical `AppDbContext` for EF Core.
- Migrations in the wrong project will not update the real database schema and will cause runtime errors.

#### Checklist for Contributors
- [ ] Are all migration files in `src/Emma.Data/Migrations`?
- [ ] Did you use the correct `--project` and `--startup-project` flags?
- [ ] Is your migration tracked in source control?
- [ ] Did you update the actual target database after creating the migration?
- [ ] Did you review the migration diff for correctness?

---

### 1. Create and apply a migration
`powershell
.\Run-Migration.ps1 -CreateMigration -UpdateDatabase
2. Or just apply existing migrations
powershell
CopyInsert in Terminal
.\Run-Migration.ps1 -UpdateDatabase
Connection Help
If you see "No such host is known":
Use postgres if running in Docker
Use localhost if running on your computer
Example Connection Strings:
Docker:
CopyInsert
Server=postgres;Port=5432;Database=emma;User Id=postgres;Password=postgres;
Your Computer:
CopyInsert
Server=localhost;Port=5432;Database=emma;User Id=postgres;Password=postgres;
Fix Common Issues
If you get a permission error:
powershell
CopyInsert in Terminal
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope Process
If EF tools are missing:
powershell
CopyInsert in Terminal
dotnet tool install --global dotnet-ef
