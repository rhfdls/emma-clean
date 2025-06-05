# Simple script for Emma AI Platform
cd Emma.Data
dotnet ef migrations script --startup-project ../Emma.Api --output ../emma-migrations.sql
cd ..
