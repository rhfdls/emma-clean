using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Npgsql;
using Microsoft.Azure.Cosmos;

namespace Emma.SeedImport
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var config = JObject.Parse(File.ReadAllText("appsettings.json"));
            var seedPath = (string)config["SeedFilePath"];
            var seedData = JObject.Parse(File.ReadAllText(seedPath));

            await ImportToPostgres(seedData, config["Postgres"]["ConnectionString"].ToString());
            await ImportToCosmosDb(seedData, config["CosmosDb"]);
            Console.WriteLine("Seed data imported to Postgres and CosmosDB.");
        }

        static async Task ImportToPostgres(JObject seed, string connStr)
        {
            using var conn = new NpgsqlConnection(connStr);
            await conn.OpenAsync();
            using var tx = conn.BeginTransaction();

            // AGENTS
            foreach (var agent in seed["agents"])
            {
                await using var cmd = new NpgsqlCommand(@"
                    INSERT INTO agents (id, external_ids, name, email, roles, organization_id, status, created_at)
                    VALUES (@id, @external_ids, @name, @email, @roles, @organization_id, @status, @created_at)
                    ON CONFLICT (id) DO NOTHING;", conn, tx);
                cmd.Parameters.AddWithValue("id", (string)agent["id"]);
                cmd.Parameters.AddWithValue("external_ids", agent["externalIds"].ToString());
                cmd.Parameters.AddWithValue("name", (string)agent["name"]);
                cmd.Parameters.AddWithValue("email", (string)agent["email"]);
                cmd.Parameters.AddWithValue("roles", agent["roles"].ToObject<string[]>());
                cmd.Parameters.AddWithValue("organization_id", (string)agent["organizationId"] ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("status", (string)agent["status"]);
                cmd.Parameters.AddWithValue("created_at", DateTime.Parse((string)agent["createdAt"]));
                await cmd.ExecuteNonQueryAsync();
            }

            // CONTACTS
            foreach (var contact in seed["contacts"])
            {
                await using var cmd = new NpgsqlCommand(@"
                    INSERT INTO contacts (id, external_ids, first_name, last_name, emails, phones, address, tags, lead_source, owner_id, created_at, updated_at, custom_fields, privacy_level)
                    VALUES (@id, @external_ids, @first_name, @last_name, @emails, @phones, @address, @tags, @lead_source, @owner_id, @created_at, @updated_at, @custom_fields, @privacy_level)
                    ON CONFLICT (id) DO NOTHING;", conn, tx);
                cmd.Parameters.AddWithValue("id", (string)contact["id"]);
                cmd.Parameters.AddWithValue("external_ids", contact["externalIds"].ToString());
                cmd.Parameters.AddWithValue("first_name", (string)contact["firstName"]);
                cmd.Parameters.AddWithValue("last_name", (string)contact["lastName"]);
                cmd.Parameters.AddWithValue("emails", contact["emails"].ToString());
                cmd.Parameters.AddWithValue("phones", contact["phones"].ToString());
                cmd.Parameters.AddWithValue("address", contact["address"].ToString());
                cmd.Parameters.AddWithValue("tags", contact["tags"].ToObject<string[]>());
                cmd.Parameters.AddWithValue("lead_source", (string)contact["leadSource"] ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("owner_id", (string)contact["ownerId"]);
                cmd.Parameters.AddWithValue("created_at", DateTime.Parse((string)contact["createdAt"]));
                cmd.Parameters.AddWithValue("updated_at", contact["updatedAt"] != null ? DateTime.Parse((string)contact["updatedAt"]) : (object)DBNull.Value);
                cmd.Parameters.AddWithValue("custom_fields", contact["customFields"].ToString());
                cmd.Parameters.AddWithValue("privacy_level", (string)contact["privacyLevel"] ?? "public");
                await cmd.ExecuteNonQueryAsync();
            }

            // INTERACTIONS
            foreach (var interaction in seed["interactions"])
            {
                await using var cmd = new NpgsqlCommand(@"
                    INSERT INTO interactions (id, contact_id, external_ids, type, direction, timestamp, agent_id, content, channel, status, related_entities, tags, custom_fields)
                    VALUES (@id, @contact_id, @external_ids, @type, @direction, @timestamp, @agent_id, @content, @channel, @status, @related_entities, @tags, @custom_fields)
                    ON CONFLICT (id) DO NOTHING;", conn, tx);
                cmd.Parameters.AddWithValue("id", (string)interaction["id"]);
                cmd.Parameters.AddWithValue("contact_id", (string)interaction["contactId"]);
                cmd.Parameters.AddWithValue("external_ids", interaction["externalIds"].ToString());
                cmd.Parameters.AddWithValue("type", (string)interaction["type"]);
                cmd.Parameters.AddWithValue("direction", (string)interaction["direction"]);
                cmd.Parameters.AddWithValue("timestamp", DateTime.Parse((string)interaction["timestamp"]));
                cmd.Parameters.AddWithValue("agent_id", (string)interaction["agentId"] ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("content", (string)interaction["content"] ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("channel", (string)interaction["channel"] ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("status", (string)interaction["status"] ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("related_entities", interaction["relatedEntities"].ToString());
                cmd.Parameters.AddWithValue("tags", interaction["tags"].ToObject<string[]>());
                cmd.Parameters.AddWithValue("custom_fields", interaction["customFields"].ToString());
                await cmd.ExecuteNonQueryAsync();
            }

            await tx.CommitAsync();
        }

        static async Task ImportToCosmosDb(JObject seed, JToken cosmosConfig)
        {
            var endpoint = cosmosConfig["AccountEndpoint"].ToString();
            var key = cosmosConfig["AccountKey"].ToString();
            var dbName = cosmosConfig["DatabaseName"].ToString();
            var containers = cosmosConfig["Containers"];
            var client = new CosmosClient(endpoint, key);
            var dbResponse = await client.CreateDatabaseIfNotExistsAsync(dbName);
            var db = dbResponse.Database;
            foreach (var entity in new[] { "agents", "contacts", "interactions" })
            {
                var containerName = containers[entity].ToString();
                var containerResponse = await db.CreateContainerIfNotExistsAsync(containerName, "/id");
                var container = containerResponse.Container;
                foreach (var item in seed[entity])
                {
                    await container.UpsertItemAsync(item, new Microsoft.Azure.Cosmos.PartitionKey((string)item["id"]));
                }
            }
        }
    }
}
