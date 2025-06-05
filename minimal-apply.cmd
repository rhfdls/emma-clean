@echo off
echo Emma AI Platform Database Migration - Minimal Approach
cd Emma.Data
dotnet ef database update --startup-project ../Emma.Api
cd ..
echo Command complete. Check VS Code database explorer to verify tables.
