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

        [Fact(DisplayName = "List filters: ownerId, relationshipState, q with archived behavior")] // SPRINT3-TESTS
        public async Task List_Filters_Work_With_Archived_Behavior()
        {
            var factory = _factory.WithWebHostBuilder(b =>
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
                b.ConfigureAppConfiguration((ctx, cfg) =>
                {
                    Environment.SetEnvironmentVariable("ALLOW_DEV_AUTOPROVISION", "true");
                    Environment.SetEnvironmentVariable("Jwt__Issuer", "emma-dev");
                    Environment.SetEnvironmentVariable("Jwt__Audience", "emma-dev");
                    Environment.SetEnvironmentVariable("Jwt__Key", "supersecret_dev_jwt_key_please_change");
                });
            });
            var client = factory.CreateClient();

            // Admin token in a fresh org
            var orgName = $"OrgListFilter-{Guid.NewGuid():N}";
            var tokenResp = await client.PostAsJsonAsync("/api/auth/dev-token", new { email = "owner4@test.dev", organizationName = orgName, autoProvision = true });
            tokenResp.EnsureSuccessStatusCode();
            using var tokenDoc = await JsonDocument.ParseAsync(await tokenResp.Content.ReadAsStreamAsync());
            string token = tokenDoc.RootElement.GetProperty("token").GetString()!;
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Create three contacts with varying names and states
            var c1 = await client.PostAsJsonAsync("/api/contacts", new { firstName = "Alice", lastName = "Smith", relationshipState = "Client" }); c1.EnsureSuccessStatusCode();
            var c2 = await client.PostAsJsonAsync("/api/contacts", new { firstName = "Bob", lastName = "Jones", relationshipState = "Lead" }); c2.EnsureSuccessStatusCode();
            var c3 = await client.PostAsJsonAsync("/api/contacts", new { firstName = "Charlie", lastName = "Smalls", relationshipState = "Prospect" }); c3.EnsureSuccessStatusCode();

            // Extract ids
            using var d1 = await JsonDocument.ParseAsync(await c1.Content.ReadAsStreamAsync());
            using var d2 = await JsonDocument.ParseAsync(await c2.Content.ReadAsStreamAsync());
            using var d3 = await JsonDocument.ParseAsync(await c3.Content.ReadAsStreamAsync());
            var id1 = d1.RootElement.GetProperty("id").GetGuid();
            var id2 = d2.RootElement.GetProperty("id").GetGuid();
            var id3 = d3.RootElement.GetProperty("id").GetGuid();

            // Archive one contact (id2)
            var arch = await client.PatchAsync($"/api/contacts/{id2}/archive", null);
            Assert.Equal(HttpStatusCode.NoContent, arch.StatusCode);

            // Default list excludes archived (search q=jo for Jones)
            var listQ = await client.GetAsync("/api/contacts?q=jo");
            listQ.EnsureSuccessStatusCode();
            var qBody = await listQ.Content.ReadAsStringAsync();
            Assert.DoesNotContain(id2.ToString(), qBody); // archived excluded

            // includeArchived=true shows it
            var listInc = await client.GetAsync("/api/contacts?q=jo&includeArchived=true");
            listInc.EnsureSuccessStatusCode();
            var incBody = await listInc.Content.ReadAsStringAsync();
            Assert.Contains(id2.ToString(), incBody);

            // Filter by relationshipState=Client should include Alice Smith only
            var listState = await client.GetAsync("/api/contacts?relationshipState=Client");
            listState.EnsureSuccessStatusCode();
            var stateBody = await listState.Content.ReadAsStringAsync();
            Assert.Contains(id1.ToString(), stateBody);
            Assert.DoesNotContain(id2.ToString(), stateBody);
            Assert.DoesNotContain(id3.ToString(), stateBody);
        }

        [Fact(DisplayName = "Archive and Restore flow excludes from default list and includes when requested")] // SPRINT3-TESTS
        public async Task Archive_And_Restore_Flow()
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
                b.ConfigureAppConfiguration((ctx, cfg) =>
                {
                    Environment.SetEnvironmentVariable("ALLOW_DEV_AUTOPROVISION", "true");
                    Environment.SetEnvironmentVariable("Jwt__Issuer", "emma-dev");
                    Environment.SetEnvironmentVariable("Jwt__Audience", "emma-dev");
                    Environment.SetEnvironmentVariable("Jwt__Key", "supersecret_dev_jwt_key_please_change");
                });
            });
            var client = configuredFactory.CreateClient();

            // Dev token
            var orgName1 = $"OrgArchiveTest-{Guid.NewGuid():N}";
            var devTokenResp = await client.PostAsJsonAsync("/api/auth/dev-token", new { email = "owner2@test.dev", organizationName = orgName1, autoProvision = true });
            devTokenResp.EnsureSuccessStatusCode();
            using var tokenDoc = await JsonDocument.ParseAsync(await devTokenResp.Content.ReadAsStreamAsync());
            string token = tokenDoc.RootElement.GetProperty("token").GetString()!;
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Create a contact
            var create = await client.PostAsJsonAsync("/api/contacts", new { firstName = "Arch", lastName = "Ivable" });
            create.EnsureSuccessStatusCode();
            using var createdDoc = await JsonDocument.ParseAsync(await create.Content.ReadAsStreamAsync());
            var contactId = createdDoc.RootElement.GetProperty("id").GetGuid();

            // Archive
            var archive = await client.PatchAsync($"/api/contacts/{contactId}/archive", null);
            Assert.Equal(HttpStatusCode.NoContent, archive.StatusCode);

            // Default list excludes
            var listDefault = await client.GetAsync("/api/contacts");
            listDefault.EnsureSuccessStatusCode();
            using (var arr = await JsonDocument.ParseAsync(await listDefault.Content.ReadAsStreamAsync()))
            {
                var contains = false;
                foreach (var el in arr.RootElement.EnumerateArray())
                {
                    if (el.TryGetProperty("id", out var idProp) && idProp.GetGuid() == contactId)
                    {
                        contains = true; break;
                    }
                }
                Assert.False(contains, "Archived contact should not appear in default list");
            }

            // Admin can include archived
            var listInclude = await client.GetAsync("/api/contacts?includeArchived=true");
            listInclude.EnsureSuccessStatusCode();
            using (var arr2 = await JsonDocument.ParseAsync(await listInclude.Content.ReadAsStreamAsync()))
            {
                var contains = false;
                foreach (var el in arr2.RootElement.EnumerateArray())
                {
                    if (el.TryGetProperty("id", out var idProp) && idProp.GetGuid() == contactId)
                    {
                        contains = true; break;
                    }
                }
                Assert.True(contains, "Archived contact should appear when includeArchived=true for admin");
            }

            // Restore
            var restore = await client.PatchAsync($"/api/contacts/{contactId}/restore", null);
            Assert.Equal(HttpStatusCode.OK, restore.StatusCode);

            // Default list includes again
            var listAfterRestore = await client.GetAsync("/api/contacts");
            listAfterRestore.EnsureSuccessStatusCode();
            using (var arr3 = await JsonDocument.ParseAsync(await listAfterRestore.Content.ReadAsStreamAsync()))
            {
                var contains = false;
                foreach (var el in arr3.RootElement.EnumerateArray())
                {
                    if (el.TryGetProperty("id", out var idProp) && idProp.GetGuid() == contactId)
                    {
                        contains = true; break;
                    }
                }
                Assert.True(contains, "Restored contact should appear in default list");
            }
        }

        [Fact(DisplayName = "Hard delete requires reason and purges contact")] // SPRINT3-TESTS
        public async Task Hard_Delete_Requires_Reason_And_Purges()
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
                b.ConfigureAppConfiguration((ctx, cfg) =>
                {
                    Environment.SetEnvironmentVariable("ALLOW_DEV_AUTOPROVISION", "true");
                    Environment.SetEnvironmentVariable("Jwt__Issuer", "emma-dev");
                    Environment.SetEnvironmentVariable("Jwt__Audience", "emma-dev");
                    Environment.SetEnvironmentVariable("Jwt__Key", "supersecret_dev_jwt_key_please_change");
                });
            });
            var client = configuredFactory.CreateClient();

            // Dev token
            var orgName2 = $"OrgDeleteTest-{Guid.NewGuid():N}";
            var devTokenResp = await client.PostAsJsonAsync("/api/auth/dev-token", new { email = "owner3@test.dev", organizationName = orgName2, autoProvision = true });
            devTokenResp.EnsureSuccessStatusCode();
            using var tokenDoc = await JsonDocument.ParseAsync(await devTokenResp.Content.ReadAsStreamAsync());
            string token = tokenDoc.RootElement.GetProperty("token").GetString()!;
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Create contact
            var create = await client.PostAsJsonAsync("/api/contacts", new { firstName = "Del", lastName = "Etable" });
            create.EnsureSuccessStatusCode();
            using var createdDoc = await JsonDocument.ParseAsync(await create.Content.ReadAsStreamAsync());
            var contactId = createdDoc.RootElement.GetProperty("id").GetGuid();

            // Missing reason → 400
            var delNoReason = await client.DeleteAsync($"/api/contacts/{contactId}?mode=hard");
            Assert.Equal(HttpStatusCode.BadRequest, delNoReason.StatusCode);

            // With reason → 204, then GET 404
            var del = await client.DeleteAsync($"/api/contacts/{contactId}?mode=hard&reason=subject-erasure");
            Assert.Equal(HttpStatusCode.NoContent, del.StatusCode);

            var get = await client.GetAsync($"/api/contacts/{contactId}");
            Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
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
                b.ConfigureAppConfiguration((ctx, cfg) =>
                {
                    // Ensure Program.cs forces dev JWT defaults for tests
                    Environment.SetEnvironmentVariable("ALLOW_DEV_AUTOPROVISION", "true");
                    // Also set environment variables for Jwt to ensure precedence in Program.cs
                    Environment.SetEnvironmentVariable("Jwt__Issuer", "emma-dev");
                    Environment.SetEnvironmentVariable("Jwt__Audience", "emma-dev");
                    Environment.SetEnvironmentVariable("Jwt__Key", "supersecret_dev_jwt_key_please_change");
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
            if (create.StatusCode != HttpStatusCode.Created)
            {
                var body = await create.Content.ReadAsStringAsync();
                Assert.True(false, $"Expected 201 Created but got {(int)create.StatusCode} {create.StatusCode}. Body: {body}");
            }
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
