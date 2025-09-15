CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250812161632_AddOrganizationInvitations') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20250812161632_AddOrganizationInvitations', '8.0.8');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250813233428_AddOrganizationPlanId') THEN
    ALTER TABLE "Organizations" ADD "PlanId" text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250813233428_AddOrganizationPlanId') THEN
    ALTER TABLE "Users"
                      ADD COLUMN IF NOT EXISTS "VerifiedAt" timestamptz NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250813233428_AddOrganizationPlanId') THEN
    CREATE INDEX IF NOT EXISTS "IX_Organizations_PlanId"
                      ON "Organizations" ("PlanId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250813233428_AddOrganizationPlanId') THEN
    UPDATE "Organizations"
                        SET "PlanId" = CASE "PlanType"
                            WHEN 0 THEN 'Free'
                            WHEN 1 THEN 'Basic'
                            WHEN 2 THEN 'Pro'
                            WHEN 3 THEN 'Enterprise'
                            ELSE "PlanId"
                        END
                      WHERE "PlanId" IS NULL
                        AND "PlanType" IS NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250813233428_AddOrganizationPlanId') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20250813233428_AddOrganizationPlanId', '8.0.8');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250814012809_MakePlanIdRequired') THEN
    UPDATE "Organizations"
                      SET "PlanId" = COALESCE("PlanId", 'free')
                      WHERE "PlanId" IS NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250814012809_MakePlanIdRequired') THEN
    ALTER TABLE "Organizations"
                      ALTER COLUMN "PlanId" SET DEFAULT 'free';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250814012809_MakePlanIdRequired') THEN
    ALTER TABLE "Organizations"
                      ALTER COLUMN "PlanId" SET NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250814012809_MakePlanIdRequired') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20250814012809_MakePlanIdRequired', '8.0.8');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250814012926_EnforcePlanIdNotNull') THEN
    UPDATE "Organizations"
                      SET "PlanId" = COALESCE("PlanId", 'free')
                      WHERE "PlanId" IS NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250814012926_EnforcePlanIdNotNull') THEN
    ALTER TABLE "Organizations"
                      ALTER COLUMN "PlanId" SET DEFAULT 'free';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250814012926_EnforcePlanIdNotNull') THEN
    ALTER TABLE "Organizations"
                      ALTER COLUMN "PlanId" SET NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250814012926_EnforcePlanIdNotNull') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20250814012926_EnforcePlanIdNotNull', '8.0.8');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250814014921_AddUsersVerifiedConsistency') THEN
    -- Ensure IsVerified is never NULL
    UPDATE "Users" SET "IsVerified" = FALSE WHERE "IsVerified" IS NULL;

    -- If user marked verified but missing timestamp, set VerifiedAt to now
    UPDATE "Users" SET "VerifiedAt" = NOW() AT TIME ZONE 'UTC' WHERE "IsVerified" = TRUE AND "VerifiedAt" IS NULL;

    -- If user not verified but has a timestamp, null it out
    UPDATE "Users" SET "VerifiedAt" = NULL WHERE "IsVerified" = FALSE AND "VerifiedAt" IS NOT NULL;

    -- Add check constraint idempotently
    DO $$
    BEGIN
        IF NOT EXISTS (
            SELECT 1 FROM pg_constraint c
            JOIN pg_class t ON t.oid = c.conrelid
            WHERE c.conname = 'users_verified_consistency'
              AND t.relname = 'Users'
        ) THEN
            ALTER TABLE "Users"
            ADD CONSTRAINT users_verified_consistency
            CHECK ( ("IsVerified" = FALSE AND "VerifiedAt" IS NULL)
                 OR ("IsVerified" = TRUE  AND "VerifiedAt" IS NOT NULL) );
        END IF;
    END $$;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250814014921_AddUsersVerifiedConsistency') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20250814014921_AddUsersVerifiedConsistency', '8.0.8');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250814015201_HardenOrganizationCustomFields') THEN
    -- Ensure Organizations.CustomFields exists as jsonb with default and not null
    DO $$
    BEGIN
        IF NOT EXISTS (
            SELECT 1
            FROM information_schema.columns
            WHERE table_name = 'Organizations' AND column_name = 'CustomFields'
        ) THEN
            -- Create the column with default and not null
            ALTER TABLE "Organizations" ADD COLUMN "CustomFields" jsonb NOT NULL DEFAULT '{}'::jsonb;
        ELSE
            -- Backfill nulls to empty jsonb
            UPDATE "Organizations" SET "CustomFields" = '{}'::jsonb WHERE "CustomFields" IS NULL;

            -- Set default '{}'::jsonb if not already set
            IF NOT EXISTS (
                SELECT 1
                FROM pg_attrdef d
                JOIN pg_class t ON t.oid = d.adrelid
                JOIN pg_attribute a ON a.attrelid = d.adrelid AND a.attnum = d.adnum
                WHERE t.relname = 'Organizations' AND a.attname = 'CustomFields'
            ) THEN
                ALTER TABLE "Organizations" ALTER COLUMN "CustomFields" SET DEFAULT '{}'::jsonb;
            END IF;

            -- Enforce NOT NULL
            ALTER TABLE "Organizations" ALTER COLUMN "CustomFields" SET NOT NULL;
        END IF;
    END $$;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250814015201_HardenOrganizationCustomFields') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20250814015201_HardenOrganizationCustomFields', '8.0.8');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250826021740_Sync_User_Model_2025Q3') THEN
    ALTER TABLE "Interactions" ADD analysis_json jsonb;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250826021740_Sync_User_Model_2025Q3') THEN
    ALTER TABLE "Interactions" ADD analysis_run_log_json jsonb;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250826021740_Sync_User_Model_2025Q3') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20250826021740_Sync_User_Model_2025Q3', '8.0.8');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250908213000_AddOrganizationsIsActive') THEN
    ALTER TABLE "Organizations" ADD "IsActive" boolean NOT NULL DEFAULT TRUE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250908213000_AddOrganizationsIsActive') THEN
    CREATE INDEX "IX_Organizations_IsActive" ON "Organizations" ("IsActive");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250908213000_AddOrganizationsIsActive') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20250908213000_AddOrganizationsIsActive', '8.0.8');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250908233638_AddUserLastLoginAt') THEN
    ALTER TABLE "Users" ADD "LastLoginAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250908233638_AddUserLastLoginAt') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20250908233638_AddUserLastLoginAt', '8.0.8');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250908235959_AddUserProfileFields') THEN
    ALTER TABLE "Users" ADD "Locale" text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250908235959_AddUserProfileFields') THEN
    ALTER TABLE "Users" ADD "TimeZone" text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250908235959_AddUserProfileFields') THEN
    ALTER TABLE "Users" ADD "ProfileImageUrl" text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250908235959_AddUserProfileFields') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20250908235959_AddUserProfileFields', '8.0.8');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250909175500_SetUsersIsActiveDefaultTrue') THEN
    UPDATE "Users" SET "IsActive" = TRUE WHERE "IsActive" IS NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250909175500_SetUsersIsActiveDefaultTrue') THEN
    ALTER TABLE "Users" ALTER COLUMN "IsActive" SET DEFAULT TRUE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250909175500_SetUsersIsActiveDefaultTrue') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20250909175500_SetUsersIsActiveDefaultTrue', '8.0.8');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250909180600_SetOrganizationsPlanIdDefaultBasic') THEN
    UPDATE "Organizations" SET "PlanId" = 'Basic' WHERE "PlanId" IS NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250909180600_SetOrganizationsPlanIdDefaultBasic') THEN
    ALTER TABLE "Organizations" ALTER COLUMN "PlanId" SET DEFAULT 'Basic';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250909180600_SetOrganizationsPlanIdDefaultBasic') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20250909180600_SetOrganizationsPlanIdDefaultBasic', '8.0.8');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250909184500_AddUniqueIndex_User_Email_PerOrg') THEN

    WITH ranked AS (
      SELECT "Id", "OrganizationId", lower("Email") AS email_norm,
             ROW_NUMBER() OVER (PARTITION BY "OrganizationId", lower("Email") ORDER BY "CreatedAt" DESC, "Id" DESC) AS rn
      FROM "Users"
    )
    DELETE FROM "Users" u
    USING ranked r
    WHERE u."Id" = r."Id" AND r.rn > 1;

    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250909184500_AddUniqueIndex_User_Email_PerOrg') THEN
    CREATE UNIQUE INDEX IF NOT EXISTS ux_users_org_lower_email ON "Users" ("OrganizationId", lower("Email"));
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250909184500_AddUniqueIndex_User_Email_PerOrg') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20250909184500_AddUniqueIndex_User_Email_PerOrg', '8.0.8');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250913221551_S1_AddIndexes') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20250913221551_S1_AddIndexes', '8.0.8');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250914174112_S2_AddArchiveAndAudit') THEN
    ALTER TABLE "Contacts" ADD "ArchivedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250914174112_S2_AddArchiveAndAudit') THEN
    ALTER TABLE "Contacts" ADD "DeletedByUserId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250914174112_S2_AddArchiveAndAudit') THEN
    ALTER TABLE "Contacts" ADD "IsArchived" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250914174112_S2_AddArchiveAndAudit') THEN
    CREATE TABLE "AuditEvents" (
        "Id" uuid NOT NULL,
        "OrganizationId" uuid NOT NULL,
        "ActorUserId" uuid,
        "Action" character varying(100) NOT NULL,
        "OccurredAt" timestamp with time zone NOT NULL,
        "TraceId" character varying(100),
        "DetailsJson" jsonb,
        CONSTRAINT "PK_AuditEvents" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250914174112_S2_AddArchiveAndAudit') THEN
    CREATE INDEX "IX_Contacts_OrganizationId_IsArchived_OwnerId" ON "Contacts" ("OrganizationId", "IsArchived", "OwnerId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250914174112_S2_AddArchiveAndAudit') THEN
    CREATE INDEX "IX_AuditEvents_Action" ON "AuditEvents" ("Action");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250914174112_S2_AddArchiveAndAudit') THEN
    CREATE INDEX "IX_AuditEvents_OccurredAt" ON "AuditEvents" ("OccurredAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250914174112_S2_AddArchiveAndAudit') THEN
    CREATE INDEX "IX_AuditEvents_OrganizationId" ON "AuditEvents" ("OrganizationId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250914174112_S2_AddArchiveAndAudit') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20250914174112_S2_AddArchiveAndAudit', '8.0.8');
    END IF;
END $EF$;
COMMIT;

