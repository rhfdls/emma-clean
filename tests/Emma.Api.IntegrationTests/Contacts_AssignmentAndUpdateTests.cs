using System;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Emma.Infrastructure.Data;
using Emma.Models.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using System.Collections.Generic;
using Xunit;

namespace Emma.Api.IntegrationTests
{
    public class Contacts_AssignmentAndUpdateTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        public Contacts_AssignmentAndUpdateTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
        }

        [Fact(DisplayName = "Owner assigns; collaborator limited to business-only fields")] // SPRINT3-TESTS
        public async Task Owner_Assigns_Then_Collaborator_BusinessOnly()
        {
            var configuredFactory = _factory.WithWebHostBuilder(b =>
            {
                b.UseEnvironment("Development");
                b.ConfigureAppConfiguration((ctx, cfg) =>
                {
                    cfg.AddInMemoryCollection(new Dictionary<string, string>
                    {
                        ["ALLOW_DEV_AUTOPROVISION"] = "true",
                        ["Jwt:Issuer"] = "emma-dev",
                        ["Jwt:Audience"] = "emma-dev",
                        ["Jwt:Key"] = "supersecret_dev_jwt_key_please_change"
                    });
                });
            });
            var client = configuredFactory.CreateClient();

            // 1) Dev token → capture Bearer + ids
            var devTokenResp = await client.PostAsJsonAsync("/api/auth/dev-token", new { email = "owner@test.dev", organizationName = "OrgA" });
            devTokenResp.EnsureSuccessStatusCode();
            using var tokenDoc = await JsonDocument.ParseAsync(await devTokenResp.Content.ReadAsStreamAsync());
            string token = tokenDoc.RootElement.GetProperty("token").GetString()!;
            Guid ownerUserId = tokenDoc.RootElement.GetProperty("userId").GetGuid();
            Guid orgId = tokenDoc.RootElement.GetProperty("orgId").GetGuid();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // 2) Create contact
            var create = await client.PostAsJsonAsync("/api/contacts", new {
                firstName = "Ada", lastName = "Lovelace", primaryEmail = "ada@example.com"
            });
            Assert.Equal(HttpStatusCode.Created, create.StatusCode);
            using var createdDoc = await JsonDocument.ParseAsync(await create.Content.ReadAsStreamAsync());
            Guid contactId = createdDoc.RootElement.GetProperty("id").GetGuid();

            // 3) Add collaborator (seed directly in DB for now)
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<EmmaDbContext>();
                var collabId = Guid.NewGuid();
                var now = DateTime.UtcNow;
                // Insert via raw SQL to include UpdatedAt despite EF Computed configuration
                var sql = @"INSERT INTO ""ContactCollaborators"" (
                                ""Id"", ""CreatedAt"", ""UpdatedAt"", ""ContactId"", ""CollaboratorUserId"", ""GrantedByUserId"", ""OrganizationId"", ""Role"", ""IsActive"",
                                ""CanAccessBusinessInteractions"", ""CanAccessPersonalInteractions"", ""CanCreateInteractions"", ""CanEditInteractions"",
                                ""CanAssignResources"", ""CanAccessFinancialData"", ""CanEditContactDetails"", ""CanManageCollaborators"", ""CanViewAuditLogs""
                            ) VALUES (
                                @p0, @p1, @p2, @p3, @p4, @p5, @p6, @p7, @p8,
                                @p9, @p10, @p11, @p12, @p13, @p14, @p15, @p16, @p17
                            );";
                await db.Database.ExecuteSqlRawAsync(sql,
                    collabId,
                    now,
                    now,
                    contactId,
                    ownerUserId,
                    ownerUserId,
                    orgId,
                    (int)CollaboratorRole.Assistant,
                    true,
                    /* permissions (Assistant defaults) */
                    true,   /* CanAccessBusinessInteractions */
                    false,  /* CanAccessPersonalInteractions */
                    false,  /* CanCreateInteractions */
                    false,  /* CanEditInteractions */
                    false,  /* CanAssignResources */
                    false,  /* CanAccessFinancialData */
                    false,  /* CanEditContactDetails */
                    false,  /* CanManageCollaborators */
                    false   /* CanViewAuditLogs */
                );
            }

            // 4) Owner assigns (to themselves for simplicity)
            var assign = await client.PostAsJsonAsync($"/api/contacts/{contactId}/assignments", new {
                assigneeUserId = ownerUserId, isPrimary = true
            });
            Assert.Equal(HttpStatusCode.NoContent, assign.StatusCode);

            // 5) Collaborator attempt to update business fields (simulate by same token if seeded user==owner in step 3)
            var okUpdate = await client.PutAsJsonAsync($"/api/contacts/{contactId}", new {
                companyName = "Lovelace & Co.", website = "https://lovelace.example", isPreferred = true
            });
            Assert.Equal(HttpStatusCode.OK, okUpdate.StatusCode);

            // 6) Collaborator attempt to update forbidden fields → 403 with blockedFields
            var badUpdate = await client.PutAsJsonAsync($"/api/contacts/{contactId}", new {
                relationshipState = "ServiceProvider", primaryEmail = "h@x.com", ownerId = Guid.NewGuid()
            });
            var badBody = await badUpdate.Content.ReadAsStringAsync();
            Assert.True(badUpdate.StatusCode == HttpStatusCode.Forbidden,
                $"Expected 403, got {(int)badUpdate.StatusCode} {badUpdate.StatusCode}. Body: {badBody}");
        }

        [Fact(DisplayName = "Cross-org read/update hidden")] // SPRINT3-TESTS
        public async Task CrossOrg_Is_Hidden()
        {
            var configuredFactoryA = _factory.WithWebHostBuilder(b =>
            {
                b.UseEnvironment("Development");
                b.ConfigureAppConfiguration((ctx, cfg) =>
                {
                    cfg.AddInMemoryCollection(new Dictionary<string, string>
                    {
                        ["ALLOW_DEV_AUTOPROVISION"] = "true",
                        ["Jwt:Issuer"] = "emma-dev",
                        ["Jwt:Audience"] = "emma-dev",
                        ["Jwt:Key"] = "supersecret_dev_jwt_key_please_change"
                    });
                });
            });
            var clientA = configuredFactoryA.CreateClient();
            var tokenAResp = await clientA.PostAsJsonAsync("/api/auth/dev-token", new { email = "a@test.dev", organizationName = "OrgA" });
            tokenAResp.EnsureSuccessStatusCode();
            using (var tokenADoc = await JsonDocument.ParseAsync(await tokenAResp.Content.ReadAsStreamAsync()))
            {
                clientA.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenADoc.RootElement.GetProperty("token").GetString());
            }

            var create = await clientA.PostAsJsonAsync("/api/contacts", new { firstName = "X", lastName = "Y" });
            create.EnsureSuccessStatusCode();
            using var createdDocB = await JsonDocument.ParseAsync(await create.Content.ReadAsStreamAsync());
            var contactId = createdDocB.RootElement.GetProperty("id").GetGuid();

            // Org B token
            var configuredFactoryB = _factory.WithWebHostBuilder(b =>
            {
                b.UseEnvironment("Development");
                b.ConfigureAppConfiguration((ctx, cfg) =>
                {
                    cfg.AddInMemoryCollection(new Dictionary<string, string>
                    {
                        ["ALLOW_DEV_AUTOPROVISION"] = "true",
                        ["Jwt:Issuer"] = "emma-dev",
                        ["Jwt:Audience"] = "emma-dev",
                        ["Jwt:Key"] = "supersecret_dev_jwt_key_please_change"
                    });
                });
            });
            var clientB = configuredFactoryB.CreateClient();
            var tokenBResp = await clientB.PostAsJsonAsync("/api/auth/dev-token", new { email = "b@test.dev", organizationName = "OrgB" });
            tokenBResp.EnsureSuccessStatusCode();
            using (var tokenBDoc = await JsonDocument.ParseAsync(await tokenBResp.Content.ReadAsStreamAsync()))
            {
                clientB.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenBDoc.RootElement.GetProperty("token").GetString());
            }

            var get = await clientB.GetAsync($"/api/contacts/{contactId}");
            Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);

            var put = await clientB.PutAsJsonAsync($"/api/contacts/{contactId}", new { companyName = "Nope" });
            Assert.Equal(HttpStatusCode.NotFound, put.StatusCode);
        }
    }
}
