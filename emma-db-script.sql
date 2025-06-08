CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;

CREATE TABLE "Organizations" (
    "Id" uuid NOT NULL,
    "Email" text NOT NULL,
    "FubApiKey" text NOT NULL,
    "FubSystem" text NOT NULL,
    "FubSystemKey" text NOT NULL,
    "FubId" integer,
    "CreatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_Organizations" PRIMARY KEY ("Id")
);

CREATE TABLE "Agents" (
    "Id" uuid NOT NULL,
    "FirstName" text NOT NULL,
    "LastName" text NOT NULL,
    "Email" text NOT NULL,
    "Password" text NOT NULL,
    "FubApiKey" text,
    "FubUserId" integer,
    "CreatedAt" timestamp with time zone NOT NULL,
    "IsActive" boolean NOT NULL,
    "OrganizationId" uuid,
    CONSTRAINT "PK_Agents" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Agents_Organizations_OrganizationId" FOREIGN KEY ("OrganizationId") REFERENCES "Organizations" ("Id")
);

CREATE TABLE "Conversations" (
    "Id" uuid NOT NULL,
    "ClientId" integer NOT NULL,
    "ClientFirstName" text NOT NULL,
    "ClientLastName" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "OrganizationId" uuid NOT NULL,
    "AgentId" uuid NOT NULL,
    CONSTRAINT "PK_Conversations" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Conversations_Organizations_OrganizationId" FOREIGN KEY ("OrganizationId") REFERENCES "Organizations" ("Id") ON DELETE CASCADE
);

CREATE TABLE "AgentPhoneNumbers" (
    "Id" uuid NOT NULL,
    "Number" text NOT NULL,
    "AgentId" uuid NOT NULL,
    CONSTRAINT "PK_AgentPhoneNumbers" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_AgentPhoneNumbers_Agents_AgentId" FOREIGN KEY ("AgentId") REFERENCES "Agents" ("Id") ON DELETE CASCADE
);

CREATE TABLE "DeviceTokens" (
    "AgentId" uuid NOT NULL,
    "DeviceId" uuid NOT NULL,
    "Token" text NOT NULL,
    CONSTRAINT "PK_DeviceTokens" PRIMARY KEY ("AgentId", "DeviceId"),
    CONSTRAINT "FK_DeviceTokens_Agents_AgentId" FOREIGN KEY ("AgentId") REFERENCES "Agents" ("Id") ON DELETE CASCADE
);

CREATE TABLE "PasswordResetTokens" (
    "Id" uuid NOT NULL,
    "AgentId" uuid NOT NULL,
    "Token" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UsedAt" timestamp with time zone,
    CONSTRAINT "PK_PasswordResetTokens" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_PasswordResetTokens_Agents_AgentId" FOREIGN KEY ("AgentId") REFERENCES "Agents" ("Id") ON DELETE CASCADE
);

CREATE TABLE "Subscriptions" (
    "AgentId" uuid NOT NULL,
    "StripeSubscriptionId" text,
    "Plan" integer NOT NULL,
    "StartDate" timestamp with time zone NOT NULL,
    "EndDate" timestamp with time zone,
    "Status" integer NOT NULL,
    "SeatsLimit" integer NOT NULL,
    "IsCallProcessingEnabled" boolean NOT NULL,
    CONSTRAINT "PK_Subscriptions" PRIMARY KEY ("AgentId"),
    CONSTRAINT "FK_Subscriptions_Agents_AgentId" FOREIGN KEY ("AgentId") REFERENCES "Agents" ("Id") ON DELETE CASCADE
);

CREATE TABLE "ConversationSummaries" (
    "Id" uuid NOT NULL,
    "ConversationId" uuid NOT NULL,
    "QualityScore" double precision,
    "SummaryText" text,
    CONSTRAINT "PK_ConversationSummaries" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_ConversationSummaries_Conversations_ConversationId" FOREIGN KEY ("ConversationId") REFERENCES "Conversations" ("Id") ON DELETE CASCADE
);

CREATE TABLE "Messages" (
    "Id" uuid NOT NULL,
    "Payload" text NOT NULL,
    "BlobStorageUrl" text NOT NULL,
    "Type" integer NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "OccurredAt" timestamp with time zone NOT NULL,
    "ConversationId" uuid NOT NULL,
    CONSTRAINT "PK_Messages" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Messages_Conversations_ConversationId" FOREIGN KEY ("ConversationId") REFERENCES "Conversations" ("Id") ON DELETE CASCADE
);

CREATE TABLE "CallMetadata" (
    "MessageId" uuid NOT NULL,
    "ClientPhoneNumber" text NOT NULL,
    "DurationInSeconds" integer NOT NULL,
    "DirectionBasedOnAgent" integer NOT NULL,
    "Status" integer NOT NULL,
    "ReferenceId" text NOT NULL,
    CONSTRAINT "PK_CallMetadata" PRIMARY KEY ("MessageId"),
    CONSTRAINT "FK_CallMetadata_Messages_MessageId" FOREIGN KEY ("MessageId") REFERENCES "Messages" ("Id") ON DELETE CASCADE
);

CREATE TABLE "EmmaAnalyses" (
    "Id" uuid NOT NULL,
    "MessageId" uuid NOT NULL,
    "LeadStatus" text NOT NULL,
    "RecommendedStrategy" text NOT NULL,
    "ComplianceFlags" text[] NOT NULL,
    "FollowupGuidance" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_EmmaAnalyses" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_EmmaAnalyses_Messages_MessageId" FOREIGN KEY ("MessageId") REFERENCES "Messages" ("Id") ON DELETE CASCADE
);

CREATE TABLE "Transcriptions" (
    "Id" uuid NOT NULL,
    "BlobStorageUrl" text NOT NULL,
    "Type" integer NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "MessageId" uuid NOT NULL,
    CONSTRAINT "PK_Transcriptions" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Transcriptions_Messages_MessageId" FOREIGN KEY ("MessageId") REFERENCES "Messages" ("Id") ON DELETE CASCADE
);

CREATE TABLE "AgentAssignments" (
    "Id" uuid NOT NULL,
    "EmmaAnalysisId" uuid NOT NULL,
    "AgentId" uuid NOT NULL,
    "AssignmentType" text NOT NULL,
    "AssignedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_AgentAssignments" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_AgentAssignments_EmmaAnalyses_EmmaAnalysisId" FOREIGN KEY ("EmmaAnalysisId") REFERENCES "EmmaAnalyses" ("Id") ON DELETE CASCADE
);

CREATE TABLE "EmmaTask" (
    "Id" integer GENERATED BY DEFAULT AS IDENTITY,
    "TaskType" text NOT NULL,
    "Description" text NOT NULL,
    "DueDate" timestamp with time zone NOT NULL,
    "EmmaAnalysisId" uuid,
    CONSTRAINT "PK_EmmaTask" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_EmmaTask_EmmaAnalyses_EmmaAnalysisId" FOREIGN KEY ("EmmaAnalysisId") REFERENCES "EmmaAnalyses" ("Id")
);

CREATE INDEX "IX_AgentAssignments_EmmaAnalysisId" ON "AgentAssignments" ("EmmaAnalysisId");

CREATE UNIQUE INDEX "IX_AgentPhoneNumbers_AgentId" ON "AgentPhoneNumbers" ("AgentId");

CREATE UNIQUE INDEX "IX_AgentPhoneNumbers_Number" ON "AgentPhoneNumbers" ("Number");

CREATE UNIQUE INDEX "IX_Agents_Email" ON "Agents" ("Email");

CREATE INDEX "IX_Agents_OrganizationId" ON "Agents" ("OrganizationId");

CREATE INDEX "IX_Conversations_AgentId" ON "Conversations" ("AgentId");

CREATE UNIQUE INDEX "IX_Conversations_ClientId" ON "Conversations" ("ClientId");

CREATE INDEX "IX_Conversations_OrganizationId" ON "Conversations" ("OrganizationId");

CREATE UNIQUE INDEX "IX_ConversationSummaries_ConversationId" ON "ConversationSummaries" ("ConversationId");

CREATE INDEX "IX_ConversationSummaries_QualityScore" ON "ConversationSummaries" ("QualityScore");

CREATE INDEX "IX_EmmaAnalyses_MessageId" ON "EmmaAnalyses" ("MessageId");

CREATE INDEX "IX_EmmaTask_EmmaAnalysisId" ON "EmmaTask" ("EmmaAnalysisId");

CREATE INDEX "IX_Messages_ConversationId" ON "Messages" ("ConversationId");

CREATE INDEX "IX_Messages_OccurredAt" ON "Messages" ("OccurredAt");

CREATE INDEX "IX_Messages_Type" ON "Messages" ("Type");

CREATE UNIQUE INDEX "IX_Organizations_Email" ON "Organizations" ("Email");

CREATE UNIQUE INDEX "IX_Organizations_FubApiKey" ON "Organizations" ("FubApiKey");

CREATE UNIQUE INDEX "IX_Organizations_FubSystem" ON "Organizations" ("FubSystem");

CREATE UNIQUE INDEX "IX_Organizations_FubSystemKey" ON "Organizations" ("FubSystemKey");

CREATE INDEX "IX_PasswordResetTokens_AgentId" ON "PasswordResetTokens" ("AgentId");

CREATE UNIQUE INDEX "IX_Transcriptions_MessageId" ON "Transcriptions" ("MessageId");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20250519143002_InitialCreate', '8.0.5');

COMMIT;

START TRANSACTION;

DROP INDEX "IX_Messages_OccurredAt";

DROP INDEX "IX_Messages_Type";

ALTER TABLE "Messages" ADD "AgentId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';

CREATE INDEX "IX_Messages_AgentId" ON "Messages" ("AgentId");

CREATE UNIQUE INDEX "IX_Messages_OccurredAt_Type" ON "Messages" ("OccurredAt", "Type");

ALTER TABLE "Messages" ADD CONSTRAINT "FK_Messages_Agents_AgentId" FOREIGN KEY ("AgentId") REFERENCES "Agents" ("Id") ON DELETE RESTRICT;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20250519163901_AddAgentMessageRelationshipAndUniqueMessageIndex', '8.0.5');

COMMIT;

START TRANSACTION;

ALTER TABLE "Messages" ADD "AiResponse" text;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20250522190244_AutoMigration_20250522_150205', '8.0.5');

COMMIT;

START TRANSACTION;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20250522202726_PendingChanges_20250522_162723', '8.0.5');

COMMIT;

START TRANSACTION;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20250523122349_PendingChanges_20250523_082345', '8.0.5');

COMMIT;

START TRANSACTION;

ALTER TABLE "Agents" DROP CONSTRAINT "FK_Agents_Organizations_OrganizationId";

ALTER TABLE "Conversations" DROP CONSTRAINT "FK_Conversations_Organizations_OrganizationId";

ALTER TABLE "ConversationSummaries" DROP CONSTRAINT "FK_ConversationSummaries_Conversations_ConversationId";

ALTER TABLE "Messages" DROP CONSTRAINT "FK_Messages_Conversations_ConversationId";

ALTER TABLE "Subscriptions" DROP CONSTRAINT "FK_Subscriptions_Agents_AgentId";

ALTER TABLE "Conversations" DROP CONSTRAINT "PK_Conversations";

DROP INDEX "IX_Conversations_AgentId";

DROP INDEX "IX_Conversations_ClientId";

ALTER TABLE "Subscriptions" DROP COLUMN "Plan";

ALTER TABLE "Conversations" DROP COLUMN "ClientId";

ALTER TABLE "Conversations" RENAME TO "Conversation";

ALTER TABLE "Conversation" RENAME COLUMN "CreatedAt" TO "Timestamp";

ALTER TABLE "Conversation" RENAME COLUMN "ClientLastName" TO "Type";

ALTER TABLE "Conversation" RENAME COLUMN "ClientFirstName" TO "Status";

ALTER INDEX "IX_Conversations_OrganizationId" RENAME TO "IX_Conversation_OrganizationId";

CREATE EXTENSION IF NOT EXISTS hstore;

ALTER TABLE "Subscriptions" ADD "PlanId" uuid;

ALTER TABLE "Organizations" ADD "OwnerAgentId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';

ALTER TABLE "Messages" ALTER COLUMN "ConversationId" DROP NOT NULL;

ALTER TABLE "Messages" ADD "InteractionId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';

ALTER TABLE "ConversationSummaries" ADD "InteractionId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';

ALTER TABLE "Conversation" ALTER COLUMN "OrganizationId" DROP NOT NULL;

ALTER TABLE "Conversation" ADD "Channel" text NOT NULL DEFAULT '';

ALTER TABLE "Conversation" ADD "ContactId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';

ALTER TABLE "Conversation" ADD "Content" text;

ALTER TABLE "Conversation" ADD "CustomFields" hstore;

ALTER TABLE "Conversation" ADD "Direction" text NOT NULL DEFAULT '';

ALTER TABLE "Conversation" ADD "ExternalIds" hstore;

ALTER TABLE "Conversation" ADD "Tags" text[] NOT NULL;

ALTER TABLE "Conversation" ADD CONSTRAINT "PK_Conversation" PRIMARY KEY ("Id");

CREATE TABLE "Feature" (
    "Id" uuid NOT NULL,
    "Code" text NOT NULL,
    "DisplayName" text NOT NULL,
    "Description" text,
    CONSTRAINT "PK_Feature" PRIMARY KEY ("Id")
);

CREATE TABLE "Interactions" (
    "Id" uuid NOT NULL,
    "ContactId" uuid NOT NULL,
    "ClientId" uuid NOT NULL,
    "OrganizationId" uuid NOT NULL,
    "ClientFirstName" text NOT NULL,
    "ClientLastName" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "ExternalIds" hstore,
    "Type" text NOT NULL,
    "Direction" text NOT NULL,
    "Timestamp" timestamp with time zone NOT NULL,
    "AgentId" uuid NOT NULL,
    "Content" text,
    "Channel" text NOT NULL,
    "Status" text NOT NULL,
    "Tags" text[] NOT NULL,
    "CustomFields" hstore,
    CONSTRAINT "PK_Interactions" PRIMARY KEY ("Id")
);

CREATE TABLE "SubscriptionPlan" (
    "Id" uuid NOT NULL,
    "Name" text NOT NULL,
    CONSTRAINT "PK_SubscriptionPlan" PRIMARY KEY ("Id")
);

CREATE TABLE "RelatedEntity" (
    "Id" uuid NOT NULL,
    "Type" text NOT NULL,
    "ConversationId" uuid,
    "InteractionId" uuid,
    CONSTRAINT "PK_RelatedEntity" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_RelatedEntity_Conversation_ConversationId" FOREIGN KEY ("ConversationId") REFERENCES "Conversation" ("Id"),
    CONSTRAINT "FK_RelatedEntity_Interactions_InteractionId" FOREIGN KEY ("InteractionId") REFERENCES "Interactions" ("Id")
);

CREATE TABLE "OrganizationSubscription" (
    "Id" uuid NOT NULL,
    "OrganizationId" uuid NOT NULL,
    "SubscriptionPlanId" uuid NOT NULL,
    "SeatsLimit" integer NOT NULL,
    "StripeSubscriptionId" text,
    "StartDate" timestamp with time zone NOT NULL,
    "EndDate" timestamp with time zone,
    "Status" integer NOT NULL,
    CONSTRAINT "PK_OrganizationSubscription" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_OrganizationSubscription_Organizations_OrganizationId" FOREIGN KEY ("OrganizationId") REFERENCES "Organizations" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_OrganizationSubscription_SubscriptionPlan_SubscriptionPlanId" FOREIGN KEY ("SubscriptionPlanId") REFERENCES "SubscriptionPlan" ("Id") ON DELETE CASCADE
);

CREATE TABLE "SubscriptionPlanFeature" (
    "SubscriptionPlanId" uuid NOT NULL,
    "FeatureId" uuid NOT NULL,
    CONSTRAINT "PK_SubscriptionPlanFeature" PRIMARY KEY ("SubscriptionPlanId", "FeatureId"),
    CONSTRAINT "FK_SubscriptionPlanFeature_Feature_FeatureId" FOREIGN KEY ("FeatureId") REFERENCES "Feature" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_SubscriptionPlanFeature_SubscriptionPlan_SubscriptionPlanId" FOREIGN KEY ("SubscriptionPlanId") REFERENCES "SubscriptionPlan" ("Id") ON DELETE CASCADE
);

CREATE TABLE "AgentSubscriptionAssignment" (
    "Id" uuid NOT NULL,
    "AgentId" uuid NOT NULL,
    "OrganizationSubscriptionId" uuid NOT NULL,
    "AssignedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_AgentSubscriptionAssignment" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_AgentSubscriptionAssignment_Agents_AgentId" FOREIGN KEY ("AgentId") REFERENCES "Agents" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_AgentSubscriptionAssignment_OrganizationSubscription_Organi~" FOREIGN KEY ("OrganizationSubscriptionId") REFERENCES "OrganizationSubscription" ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_Subscriptions_PlanId" ON "Subscriptions" ("PlanId");

CREATE INDEX "IX_Organizations_OwnerAgentId" ON "Organizations" ("OwnerAgentId");

CREATE INDEX "IX_Messages_InteractionId" ON "Messages" ("InteractionId");

CREATE INDEX "IX_ConversationSummaries_InteractionId" ON "ConversationSummaries" ("InteractionId");

CREATE INDEX "IX_AgentSubscriptionAssignment_AgentId" ON "AgentSubscriptionAssignment" ("AgentId");

CREATE INDEX "IX_AgentSubscriptionAssignment_OrganizationSubscriptionId" ON "AgentSubscriptionAssignment" ("OrganizationSubscriptionId");

CREATE INDEX "IX_Interactions_AgentId" ON "Interactions" ("AgentId");

CREATE UNIQUE INDEX "IX_Interactions_ClientId" ON "Interactions" ("ClientId");

CREATE INDEX "IX_OrganizationSubscription_OrganizationId" ON "OrganizationSubscription" ("OrganizationId");

CREATE INDEX "IX_OrganizationSubscription_SubscriptionPlanId" ON "OrganizationSubscription" ("SubscriptionPlanId");

CREATE INDEX "IX_RelatedEntity_ConversationId" ON "RelatedEntity" ("ConversationId");

CREATE INDEX "IX_RelatedEntity_InteractionId" ON "RelatedEntity" ("InteractionId");

CREATE INDEX "IX_SubscriptionPlanFeature_FeatureId" ON "SubscriptionPlanFeature" ("FeatureId");

ALTER TABLE "Agents" ADD CONSTRAINT "FK_Agents_Organizations_OrganizationId" FOREIGN KEY ("OrganizationId") REFERENCES "Organizations" ("Id") ON DELETE RESTRICT;

ALTER TABLE "Conversation" ADD CONSTRAINT "FK_Conversation_Organizations_OrganizationId" FOREIGN KEY ("OrganizationId") REFERENCES "Organizations" ("Id");

ALTER TABLE "ConversationSummaries" ADD CONSTRAINT "FK_ConversationSummaries_Interactions_InteractionId" FOREIGN KEY ("InteractionId") REFERENCES "Interactions" ("Id") ON DELETE CASCADE;

ALTER TABLE "Messages" ADD CONSTRAINT "FK_Messages_Conversation_ConversationId" FOREIGN KEY ("ConversationId") REFERENCES "Conversation" ("Id");

ALTER TABLE "Messages" ADD CONSTRAINT "FK_Messages_Interactions_InteractionId" FOREIGN KEY ("InteractionId") REFERENCES "Interactions" ("Id") ON DELETE CASCADE;

ALTER TABLE "Organizations" ADD CONSTRAINT "FK_Organizations_Agents_OwnerAgentId" FOREIGN KEY ("OwnerAgentId") REFERENCES "Agents" ("Id") ON DELETE RESTRICT;

ALTER TABLE "Subscriptions" ADD CONSTRAINT "FK_Subscriptions_Agents_AgentId" FOREIGN KEY ("AgentId") REFERENCES "Agents" ("Id") ON DELETE RESTRICT;

ALTER TABLE "Subscriptions" ADD CONSTRAINT "FK_Subscriptions_SubscriptionPlan_PlanId" FOREIGN KEY ("PlanId") REFERENCES "SubscriptionPlan" ("Id") ON DELETE RESTRICT;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20250527193749_SyncModelChanges', '8.0.5');

COMMIT;

