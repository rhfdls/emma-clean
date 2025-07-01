#!/bin/bash
set -e

# Wait for PostgreSQL to be ready
echo "Waiting for PostgreSQL to be ready..."
until PGPASSWORD=postgres psql -h "postgres" -U "postgres" -d "emma" -c '\q' 2>/dev/null; do
  >&2 echo "PostgreSQL is unavailable - sleeping"
  sleep 1
done

# Run migrations
echo "Running database migrations..."
dotnet ef database update --project "Emma.Data" --startup-project "Emma.Api" --no-build

# Start the application
echo "Starting application..."
exec dotnet Emma.Api.dll