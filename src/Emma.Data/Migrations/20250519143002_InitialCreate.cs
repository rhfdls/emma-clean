using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Emma.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Organizations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    FubApiKey = table.Column<string>(type: "text", nullable: false),
                    FubSystem = table.Column<string>(type: "text", nullable: false),
                    FubSystemKey = table.Column<string>(type: "text", nullable: false),
                    FubId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Organizations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Agents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FirstName = table.Column<string>(type: "text", nullable: false),
                    LastName = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    Password = table.Column<string>(type: "text", nullable: false),
                    FubApiKey = table.Column<string>(type: "text", nullable: true),
                    FubUserId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Agents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Agents_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Conversations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<int>(type: "integer", nullable: false),
                    ClientFirstName = table.Column<string>(type: "text", nullable: false),
                    ClientLastName = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    AgentId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Conversations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Conversations_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AgentPhoneNumbers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Number = table.Column<string>(type: "text", nullable: false),
                    AgentId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentPhoneNumbers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AgentPhoneNumbers_Agents_AgentId",
                        column: x => x.AgentId,
                        principalTable: "Agents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DeviceTokens",
                columns: table => new
                {
                    AgentId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeviceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Token = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceTokens", x => new { x.AgentId, x.DeviceId });
                    table.ForeignKey(
                        name: "FK_DeviceTokens_Agents_AgentId",
                        column: x => x.AgentId,
                        principalTable: "Agents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PasswordResetTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AgentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Token = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PasswordResetTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PasswordResetTokens_Agents_AgentId",
                        column: x => x.AgentId,
                        principalTable: "Agents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Subscriptions",
                columns: table => new
                {
                    AgentId = table.Column<Guid>(type: "uuid", nullable: false),
                    StripeSubscriptionId = table.Column<string>(type: "text", nullable: true),
                    Plan = table.Column<int>(type: "integer", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    SeatsLimit = table.Column<int>(type: "integer", nullable: false),
                    IsCallProcessingEnabled = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subscriptions", x => x.AgentId);
                    table.ForeignKey(
                        name: "FK_Subscriptions_Agents_AgentId",
                        column: x => x.AgentId,
                        principalTable: "Agents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ConversationSummaries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConversationId = table.Column<Guid>(type: "uuid", nullable: false),
                    QualityScore = table.Column<double>(type: "double precision", nullable: true),
                    SummaryText = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConversationSummaries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConversationSummaries_Conversations_ConversationId",
                        column: x => x.ConversationId,
                        principalTable: "Conversations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Messages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Payload = table.Column<string>(type: "text", nullable: false),
                    BlobStorageUrl = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ConversationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Messages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Messages_Conversations_ConversationId",
                        column: x => x.ConversationId,
                        principalTable: "Conversations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CallMetadata",
                columns: table => new
                {
                    MessageId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientPhoneNumber = table.Column<string>(type: "text", nullable: false),
                    DurationInSeconds = table.Column<int>(type: "integer", nullable: false),
                    DirectionBasedOnAgent = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ReferenceId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CallMetadata", x => x.MessageId);
                    table.ForeignKey(
                        name: "FK_CallMetadata_Messages_MessageId",
                        column: x => x.MessageId,
                        principalTable: "Messages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmmaAnalyses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MessageId = table.Column<Guid>(type: "uuid", nullable: false),
                    LeadStatus = table.Column<string>(type: "text", nullable: false),
                    RecommendedStrategy = table.Column<string>(type: "text", nullable: false),
                    ComplianceFlags = table.Column<List<string>>(type: "text[]", nullable: false),
                    FollowupGuidance = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmmaAnalyses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmmaAnalyses_Messages_MessageId",
                        column: x => x.MessageId,
                        principalTable: "Messages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Transcriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BlobStorageUrl = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    MessageId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transcriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Transcriptions_Messages_MessageId",
                        column: x => x.MessageId,
                        principalTable: "Messages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AgentAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmmaAnalysisId = table.Column<Guid>(type: "uuid", nullable: false),
                    AgentId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignmentType = table.Column<string>(type: "text", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AgentAssignments_EmmaAnalyses_EmmaAnalysisId",
                        column: x => x.EmmaAnalysisId,
                        principalTable: "EmmaAnalyses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmmaTask",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TaskType = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    DueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EmmaAnalysisId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmmaTask", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmmaTask_EmmaAnalyses_EmmaAnalysisId",
                        column: x => x.EmmaAnalysisId,
                        principalTable: "EmmaAnalyses",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AgentAssignments_EmmaAnalysisId",
                table: "AgentAssignments",
                column: "EmmaAnalysisId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentPhoneNumbers_AgentId",
                table: "AgentPhoneNumbers",
                column: "AgentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AgentPhoneNumbers_Number",
                table: "AgentPhoneNumbers",
                column: "Number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Agents_Email",
                table: "Agents",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Agents_OrganizationId",
                table: "Agents",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_AgentId",
                table: "Conversations",
                column: "AgentId");

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_ClientId",
                table: "Conversations",
                column: "ClientId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_OrganizationId",
                table: "Conversations",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_ConversationSummaries_ConversationId",
                table: "ConversationSummaries",
                column: "ConversationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConversationSummaries_QualityScore",
                table: "ConversationSummaries",
                column: "QualityScore");

            migrationBuilder.CreateIndex(
                name: "IX_EmmaAnalyses_MessageId",
                table: "EmmaAnalyses",
                column: "MessageId");

            migrationBuilder.CreateIndex(
                name: "IX_EmmaTask_EmmaAnalysisId",
                table: "EmmaTask",
                column: "EmmaAnalysisId");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_ConversationId",
                table: "Messages",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_OccurredAt",
                table: "Messages",
                column: "OccurredAt");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_Type",
                table: "Messages",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_Email",
                table: "Organizations",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_FubApiKey",
                table: "Organizations",
                column: "FubApiKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_FubSystem",
                table: "Organizations",
                column: "FubSystem",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_FubSystemKey",
                table: "Organizations",
                column: "FubSystemKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetTokens_AgentId",
                table: "PasswordResetTokens",
                column: "AgentId");

            migrationBuilder.CreateIndex(
                name: "IX_Transcriptions_MessageId",
                table: "Transcriptions",
                column: "MessageId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AgentAssignments");

            migrationBuilder.DropTable(
                name: "AgentPhoneNumbers");

            migrationBuilder.DropTable(
                name: "CallMetadata");

            migrationBuilder.DropTable(
                name: "ConversationSummaries");

            migrationBuilder.DropTable(
                name: "DeviceTokens");

            migrationBuilder.DropTable(
                name: "EmmaTask");

            migrationBuilder.DropTable(
                name: "PasswordResetTokens");

            migrationBuilder.DropTable(
                name: "Subscriptions");

            migrationBuilder.DropTable(
                name: "Transcriptions");

            migrationBuilder.DropTable(
                name: "EmmaAnalyses");

            migrationBuilder.DropTable(
                name: "Agents");

            migrationBuilder.DropTable(
                name: "Messages");

            migrationBuilder.DropTable(
                name: "Conversations");

            migrationBuilder.DropTable(
                name: "Organizations");
        }
    }
}
