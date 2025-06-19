-- Emma AI Platform Database Schema Query
-- Run this in Azure Portal Query Editor or any PostgreSQL client

-- List all tables in public schema
SELECT table_name, table_type
FROM information_schema.tables 
WHERE table_schema = 'public'
ORDER BY table_name;

-- Check if migrations history table exists
SELECT EXISTS (
    SELECT FROM information_schema.tables 
    WHERE table_schema = 'public' 
    AND table_name = '__EFMigrationsHistory'
) AS migrations_table_exists;

-- List applied migrations if table exists
SELECT * FROM "__EFMigrationsHistory"
WHERE EXISTS (
    SELECT FROM information_schema.tables 
    WHERE table_schema = 'public' 
    AND table_name = '__EFMigrationsHistory'
);
