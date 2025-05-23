# EMMA Database Migration Guide

## How to Use

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
