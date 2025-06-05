using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Emma.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddResourceAssignmentSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Agents_Organizations_OrganizationId",
                table: "Agents");

            migrationBuilder.DropForeignKey(
                name: "FK_Conversations_Organizations_OrganizationId",
                table: "Conversations");

            migrationBuilder.DropForeignKey(
                name: "FK_ConversationSummaries_Conversations_ConversationId",
                table: "ConversationSummaries");

            migrationBuilder.DropForeignKey(
                name: "FK_Messages_Conversations_ConversationId",
                table: "Messages");

            migrationBuilder.DropForeignKey(
                name: "FK_Subscriptions_Agents_AgentId",
                table: "Subscriptions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Conversations",
                table: "Conversations");

            migrationBuilder.DropIndex(
                name: "IX_Conversations_AgentId",
                table: "Conversations");

            migrationBuilder.DropIndex(
                name: "IX_Conversations_ClientId",
                table: "Conversations");

            migrationBuilder.DropColumn(
                name: "Plan",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "ClientId",
                table: "Conversations");

            migrationBuilder.RenameTable(
                name: "Conversations",
                newName: "Conversation");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "Conversation",
                newName: "Timestamp");

            migrationBuilder.RenameColumn(
                name: "ClientLastName",
                table: "Conversation",
                newName: "Type");

            migrationBuilder.RenameColumn(
                name: "ClientFirstName",
                table: "Conversation",
                newName: "Status");

            migrationBuilder.RenameIndex(
                name: "IX_Conversations_OrganizationId",
                table: "Conversation",
                newName: "IX_Conversation_OrganizationId");

            migrationBuilder.AddColumn<Guid>(
                name: "PlanId",
                table: "Subscriptions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OwnerAgentId",
                table: "Organizations",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<Guid>(
                name: "ConversationId",
                table: "Messages",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "InteractionId",
                table: "Messages",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "InteractionId",
                table: "ConversationSummaries",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<Guid>(
                name: "OrganizationId",
                table: "Conversation",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<string>(
                name: "Channel",
                table: "Conversation",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "ContactId",
                table: "Conversation",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "Content",
                table: "Conversation",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Dictionary<string, string>>(
                name: "CustomFields",
                table: "Conversation",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Direction",
                table: "Conversation",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Dictionary<string, string>>(
                name: "ExternalIds",
                table: "Conversation",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<List<string>>(
                name: "Tags",
                table: "Conversation",
                type: "text[]",
                nullable: false);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Conversation",
                table: "Conversation",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "Address",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Street = table.Column<string>(type: "text", nullable: false),
                    City = table.Column<string>(type: "text", nullable: false),
                    State = table.Column<string>(type: "text", nullable: false),
                    PostalCode = table.Column<string>(type: "text", nullable: false),
                    Country = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Address", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Feature",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
                    DisplayName = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Feature", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Interactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContactId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientFirstName = table.Column<string>(type: "text", nullable: false),
                    ClientLastName = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExternalIds = table.Column<Dictionary<string, string>>(type: "jsonb", nullable: true),
                    Type = table.Column<string>(type: "text", nullable: false),
                    Direction = table.Column<string>(type: "text", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AgentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: true),
                    Channel = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Tags = table.Column<List<string>>(type: "text[]", nullable: false),
                    CustomFields = table.Column<Dictionary<string, string>>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Interactions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ResourceCategories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IconName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResourceCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SubscriptionPlan",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriptionPlan", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "test_entities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_test_entities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Contact",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FirstName = table.Column<string>(type: "text", nullable: false),
                    LastName = table.Column<string>(type: "text", nullable: false),
                    AddressId = table.Column<Guid>(type: "uuid", nullable: true),
                    Tags = table.Column<List<string>>(type: "text[]", nullable: false),
                    LeadSource = table.Column<string>(type: "text", nullable: true),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CustomFields = table.Column<Dictionary<string, string>>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contact", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Contact_Address_AddressId",
                        column: x => x.AddressId,
                        principalTable: "Address",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RelatedEntity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    ConversationId = table.Column<Guid>(type: "uuid", nullable: true),
                    InteractionId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RelatedEntity", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RelatedEntity_Conversation_ConversationId",
                        column: x => x.ConversationId,
                        principalTable: "Conversation",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RelatedEntity_Interactions_InteractionId",
                        column: x => x.InteractionId,
                        principalTable: "Interactions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Resources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CompanyName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Website = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    AddressId = table.Column<Guid>(type: "uuid", nullable: true),
                    LicenseNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Specialties = table.Column<List<string>>(type: "text[]", nullable: false),
                    ServiceAreas = table.Column<List<string>>(type: "text[]", nullable: false),
                    RelationshipType = table.Column<int>(type: "integer", nullable: false),
                    Rating = table.Column<decimal>(type: "numeric", nullable: true),
                    ReviewCount = table.Column<int>(type: "integer", nullable: false),
                    IsPreferred = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    AgentId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedByAgentId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CustomFields = table.Column<Dictionary<string, string>>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Resources", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Resources_Address_AddressId",
                        column: x => x.AddressId,
                        principalTable: "Address",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Resources_Agents_AgentId",
                        column: x => x.AgentId,
                        principalTable: "Agents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Resources_Agents_CreatedByAgentId",
                        column: x => x.CreatedByAgentId,
                        principalTable: "Agents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Resources_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Resources_ResourceCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "ResourceCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OrganizationSubscription",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubscriptionPlanId = table.Column<Guid>(type: "uuid", nullable: false),
                    SeatsLimit = table.Column<int>(type: "integer", nullable: false),
                    StripeSubscriptionId = table.Column<string>(type: "text", nullable: true),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationSubscription", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrganizationSubscription_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrganizationSubscription_SubscriptionPlan_SubscriptionPlanId",
                        column: x => x.SubscriptionPlanId,
                        principalTable: "SubscriptionPlan",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SubscriptionPlanFeature",
                columns: table => new
                {
                    SubscriptionPlanId = table.Column<Guid>(type: "uuid", nullable: false),
                    FeatureId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriptionPlanFeature", x => new { x.SubscriptionPlanId, x.FeatureId });
                    table.ForeignKey(
                        name: "FK_SubscriptionPlanFeature_Feature_FeatureId",
                        column: x => x.FeatureId,
                        principalTable: "Feature",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SubscriptionPlanFeature_SubscriptionPlan_SubscriptionPlanId",
                        column: x => x.SubscriptionPlanId,
                        principalTable: "SubscriptionPlan",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmailAddress",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Address = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    Verified = table.Column<bool>(type: "boolean", nullable: false),
                    ContactId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailAddress", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmailAddress_Contact_ContactId",
                        column: x => x.ContactId,
                        principalTable: "Contact",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PhoneNumber",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Number = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    Verified = table.Column<bool>(type: "boolean", nullable: false),
                    ContactId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhoneNumber", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PhoneNumber_Contact_ContactId",
                        column: x => x.ContactId,
                        principalTable: "Contact",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ResourceAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContactId = table.Column<Guid>(type: "uuid", nullable: false),
                    ResourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignedByAgentId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Purpose = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    InteractionId = table.Column<Guid>(type: "uuid", nullable: true),
                    ClientRequest = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    AssignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FollowUpAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    WasUsed = table.Column<bool>(type: "boolean", nullable: false),
                    ClientRating = table.Column<decimal>(type: "numeric", nullable: true),
                    ClientFeedback = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    OutcomeNotes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    InternalNotes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CustomFields = table.Column<Dictionary<string, string>>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResourceAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResourceAssignments_Agents_AssignedByAgentId",
                        column: x => x.AssignedByAgentId,
                        principalTable: "Agents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ResourceAssignments_Contact_ContactId",
                        column: x => x.ContactId,
                        principalTable: "Contact",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ResourceAssignments_Interactions_InteractionId",
                        column: x => x.InteractionId,
                        principalTable: "Interactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ResourceAssignments_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ResourceAssignments_Resources_ResourceId",
                        column: x => x.ResourceId,
                        principalTable: "Resources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ResourceRecommendations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContactId = table.Column<Guid>(type: "uuid", nullable: false),
                    ResourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecommendedByAgentId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    InteractionId = table.Column<Guid>(type: "uuid", nullable: true),
                    Purpose = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    RecommendationOrder = table.Column<int>(type: "integer", nullable: false),
                    RecommendationNotes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    RecommendedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    WasSelected = table.Column<bool>(type: "boolean", nullable: false),
                    WasContacted = table.Column<bool>(type: "boolean", nullable: false),
                    ContactedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SelectedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AlternativeResourceName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    AlternativeResourceContact = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    WhyAlternativeChosen = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ClientFeedback = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CustomFields = table.Column<Dictionary<string, string>>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResourceRecommendations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResourceRecommendations_Agents_RecommendedByAgentId",
                        column: x => x.RecommendedByAgentId,
                        principalTable: "Agents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ResourceRecommendations_Contact_ContactId",
                        column: x => x.ContactId,
                        principalTable: "Contact",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ResourceRecommendations_Interactions_InteractionId",
                        column: x => x.InteractionId,
                        principalTable: "Interactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ResourceRecommendations_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ResourceRecommendations_Resources_ResourceId",
                        column: x => x.ResourceId,
                        principalTable: "Resources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AgentSubscriptionAssignment",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AgentId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationSubscriptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentSubscriptionAssignment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AgentSubscriptionAssignment_Agents_AgentId",
                        column: x => x.AgentId,
                        principalTable: "Agents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AgentSubscriptionAssignment_OrganizationSubscription_Organi~",
                        column: x => x.OrganizationSubscriptionId,
                        principalTable: "OrganizationSubscription",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_PlanId",
                table: "Subscriptions",
                column: "PlanId");

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_OwnerAgentId",
                table: "Organizations",
                column: "OwnerAgentId");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_InteractionId",
                table: "Messages",
                column: "InteractionId");

            migrationBuilder.CreateIndex(
                name: "IX_ConversationSummaries_InteractionId",
                table: "ConversationSummaries",
                column: "InteractionId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentSubscriptionAssignment_AgentId",
                table: "AgentSubscriptionAssignment",
                column: "AgentId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentSubscriptionAssignment_OrganizationSubscriptionId",
                table: "AgentSubscriptionAssignment",
                column: "OrganizationSubscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_Contact_AddressId",
                table: "Contact",
                column: "AddressId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailAddress_ContactId",
                table: "EmailAddress",
                column: "ContactId");

            migrationBuilder.CreateIndex(
                name: "IX_Interactions_AgentId",
                table: "Interactions",
                column: "AgentId");

            migrationBuilder.CreateIndex(
                name: "IX_Interactions_ClientId",
                table: "Interactions",
                column: "ClientId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationSubscription_OrganizationId",
                table: "OrganizationSubscription",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationSubscription_SubscriptionPlanId",
                table: "OrganizationSubscription",
                column: "SubscriptionPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_PhoneNumber_ContactId",
                table: "PhoneNumber",
                column: "ContactId");

            migrationBuilder.CreateIndex(
                name: "IX_RelatedEntity_ConversationId",
                table: "RelatedEntity",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_RelatedEntity_InteractionId",
                table: "RelatedEntity",
                column: "InteractionId");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceAssignments_AssignedByAgentId",
                table: "ResourceAssignments",
                column: "AssignedByAgentId");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceAssignments_ContactId_Status",
                table: "ResourceAssignments",
                columns: new[] { "ContactId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ResourceAssignments_InteractionId",
                table: "ResourceAssignments",
                column: "InteractionId");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceAssignments_OrganizationId_AssignedAt",
                table: "ResourceAssignments",
                columns: new[] { "OrganizationId", "AssignedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ResourceAssignments_ResourceId_Status",
                table: "ResourceAssignments",
                columns: new[] { "ResourceId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ResourceCategories_Name",
                table: "ResourceCategories",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ResourceCategories_SortOrder",
                table: "ResourceCategories",
                column: "SortOrder");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceRecommendations_ContactId_RecommendedAt",
                table: "ResourceRecommendations",
                columns: new[] { "ContactId", "RecommendedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ResourceRecommendations_InteractionId",
                table: "ResourceRecommendations",
                column: "InteractionId");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceRecommendations_OrganizationId_RecommendedAt",
                table: "ResourceRecommendations",
                columns: new[] { "OrganizationId", "RecommendedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ResourceRecommendations_RecommendedByAgentId",
                table: "ResourceRecommendations",
                column: "RecommendedByAgentId");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceRecommendations_ResourceId_WasSelected",
                table: "ResourceRecommendations",
                columns: new[] { "ResourceId", "WasSelected" });

            migrationBuilder.CreateIndex(
                name: "IX_Resources_AddressId",
                table: "Resources",
                column: "AddressId");

            migrationBuilder.CreateIndex(
                name: "IX_Resources_AgentId",
                table: "Resources",
                column: "AgentId");

            migrationBuilder.CreateIndex(
                name: "IX_Resources_CategoryId_IsPreferred_Rating",
                table: "Resources",
                columns: new[] { "CategoryId", "IsPreferred", "Rating" });

            migrationBuilder.CreateIndex(
                name: "IX_Resources_CreatedByAgentId",
                table: "Resources",
                column: "CreatedByAgentId");

            migrationBuilder.CreateIndex(
                name: "IX_Resources_OrganizationId_Name_CategoryId",
                table: "Resources",
                columns: new[] { "OrganizationId", "Name", "CategoryId" });

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionPlanFeature_FeatureId",
                table: "SubscriptionPlanFeature",
                column: "FeatureId");

            migrationBuilder.AddForeignKey(
                name: "FK_Agents_Organizations_OrganizationId",
                table: "Agents",
                column: "OrganizationId",
                principalTable: "Organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Conversation_Organizations_OrganizationId",
                table: "Conversation",
                column: "OrganizationId",
                principalTable: "Organizations",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ConversationSummaries_Interactions_InteractionId",
                table: "ConversationSummaries",
                column: "InteractionId",
                principalTable: "Interactions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Messages_Conversation_ConversationId",
                table: "Messages",
                column: "ConversationId",
                principalTable: "Conversation",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Messages_Interactions_InteractionId",
                table: "Messages",
                column: "InteractionId",
                principalTable: "Interactions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Organizations_Agents_OwnerAgentId",
                table: "Organizations",
                column: "OwnerAgentId",
                principalTable: "Agents",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Subscriptions_Agents_AgentId",
                table: "Subscriptions",
                column: "AgentId",
                principalTable: "Agents",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Subscriptions_SubscriptionPlan_PlanId",
                table: "Subscriptions",
                column: "PlanId",
                principalTable: "SubscriptionPlan",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Agents_Organizations_OrganizationId",
                table: "Agents");

            migrationBuilder.DropForeignKey(
                name: "FK_Conversation_Organizations_OrganizationId",
                table: "Conversation");

            migrationBuilder.DropForeignKey(
                name: "FK_ConversationSummaries_Interactions_InteractionId",
                table: "ConversationSummaries");

            migrationBuilder.DropForeignKey(
                name: "FK_Messages_Conversation_ConversationId",
                table: "Messages");

            migrationBuilder.DropForeignKey(
                name: "FK_Messages_Interactions_InteractionId",
                table: "Messages");

            migrationBuilder.DropForeignKey(
                name: "FK_Organizations_Agents_OwnerAgentId",
                table: "Organizations");

            migrationBuilder.DropForeignKey(
                name: "FK_Subscriptions_Agents_AgentId",
                table: "Subscriptions");

            migrationBuilder.DropForeignKey(
                name: "FK_Subscriptions_SubscriptionPlan_PlanId",
                table: "Subscriptions");

            migrationBuilder.DropTable(
                name: "AgentSubscriptionAssignment");

            migrationBuilder.DropTable(
                name: "EmailAddress");

            migrationBuilder.DropTable(
                name: "PhoneNumber");

            migrationBuilder.DropTable(
                name: "RelatedEntity");

            migrationBuilder.DropTable(
                name: "ResourceAssignments");

            migrationBuilder.DropTable(
                name: "ResourceRecommendations");

            migrationBuilder.DropTable(
                name: "SubscriptionPlanFeature");

            migrationBuilder.DropTable(
                name: "test_entities");

            migrationBuilder.DropTable(
                name: "OrganizationSubscription");

            migrationBuilder.DropTable(
                name: "Contact");

            migrationBuilder.DropTable(
                name: "Interactions");

            migrationBuilder.DropTable(
                name: "Resources");

            migrationBuilder.DropTable(
                name: "Feature");

            migrationBuilder.DropTable(
                name: "SubscriptionPlan");

            migrationBuilder.DropTable(
                name: "Address");

            migrationBuilder.DropTable(
                name: "ResourceCategories");

            migrationBuilder.DropIndex(
                name: "IX_Subscriptions_PlanId",
                table: "Subscriptions");

            migrationBuilder.DropIndex(
                name: "IX_Organizations_OwnerAgentId",
                table: "Organizations");

            migrationBuilder.DropIndex(
                name: "IX_Messages_InteractionId",
                table: "Messages");

            migrationBuilder.DropIndex(
                name: "IX_ConversationSummaries_InteractionId",
                table: "ConversationSummaries");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Conversation",
                table: "Conversation");

            migrationBuilder.DropColumn(
                name: "PlanId",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "OwnerAgentId",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "InteractionId",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "InteractionId",
                table: "ConversationSummaries");

            migrationBuilder.DropColumn(
                name: "Channel",
                table: "Conversation");

            migrationBuilder.DropColumn(
                name: "ContactId",
                table: "Conversation");

            migrationBuilder.DropColumn(
                name: "Content",
                table: "Conversation");

            migrationBuilder.DropColumn(
                name: "CustomFields",
                table: "Conversation");

            migrationBuilder.DropColumn(
                name: "Direction",
                table: "Conversation");

            migrationBuilder.DropColumn(
                name: "ExternalIds",
                table: "Conversation");

            migrationBuilder.DropColumn(
                name: "Tags",
                table: "Conversation");

            migrationBuilder.RenameTable(
                name: "Conversation",
                newName: "Conversations");

            migrationBuilder.RenameColumn(
                name: "Type",
                table: "Conversations",
                newName: "ClientLastName");

            migrationBuilder.RenameColumn(
                name: "Timestamp",
                table: "Conversations",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "Conversations",
                newName: "ClientFirstName");

            migrationBuilder.RenameIndex(
                name: "IX_Conversation_OrganizationId",
                table: "Conversations",
                newName: "IX_Conversations_OrganizationId");

            migrationBuilder.AddColumn<int>(
                name: "Plan",
                table: "Subscriptions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<Guid>(
                name: "ConversationId",
                table: "Messages",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "OrganizationId",
                table: "Conversations",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ClientId",
                table: "Conversations",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Conversations",
                table: "Conversations",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_AgentId",
                table: "Conversations",
                column: "AgentId");

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_ClientId",
                table: "Conversations",
                column: "ClientId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Agents_Organizations_OrganizationId",
                table: "Agents",
                column: "OrganizationId",
                principalTable: "Organizations",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Conversations_Organizations_OrganizationId",
                table: "Conversations",
                column: "OrganizationId",
                principalTable: "Organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ConversationSummaries_Conversations_ConversationId",
                table: "ConversationSummaries",
                column: "ConversationId",
                principalTable: "Conversations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Messages_Conversations_ConversationId",
                table: "Messages",
                column: "ConversationId",
                principalTable: "Conversations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Subscriptions_Agents_AgentId",
                table: "Subscriptions",
                column: "AgentId",
                principalTable: "Agents",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
