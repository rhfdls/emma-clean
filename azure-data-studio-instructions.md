# Azure Data Studio Instructions for Emma AI Platform

1. **Download Azure Data Studio** (if not already installed):
   - Visit: https://docs.microsoft.com/en-us/sql/azure-data-studio/download-azure-data-studio
   - Install the appropriate version for Windows

2. **Install PostgreSQL Extension**:
   - Open Azure Data Studio
   - Go to Extensions (Ctrl+Shift+X)
   - Search for "PostgreSQL" and install

3. **Connect to Azure PostgreSQL**:
   - Click "New Connection"
   - Connection Type: PostgreSQL
   - Server: emma-db-server.postgres.database.azure.com
   - Authentication: Password
   - Username: emmaadmin@emma-db-server
   - Password: (your password from the connection string)
   - Database: emma

4. **Execute SQL Script**:
   - Open a new query window
   - Open the generated SQL file: c:\Users\david\GitHub\WindsurfProjects\emma\emma-db-script.sql
   - Copy the entire content and paste it into the query window
   - Click "Run" to execute
