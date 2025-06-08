using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Nodes;
using Npgsql;
using Microsoft.Azure.Cosmos;

namespace Emma.SeedImport
{
    class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
                Console.WriteLine("[DEBUG] Start Main");
                Console.WriteLine($"[DEBUG] CWD: {Directory.GetCurrentDirectory()}");

                var configPath = "Emma.Api/Tools/appsettings.json";
                Console.WriteLine($"[DEBUG] Reading config from: {configPath}");
                var config = JsonNode.Parse(File.ReadAllText(configPath)).AsObject();
                Console.WriteLine("[DEBUG] Loaded config");

                var seedPath = config["SeedFilePath"]!.GetValue<string>();
                Console.WriteLine($"[DEBUG] Seed file path: {seedPath}");
                var seedData = JsonNode.Parse(File.ReadAllText(seedPath)).AsObject();
                Console.WriteLine("[DEBUG] Loaded seed data");

                var pgConn = config["Postgres"]!["ConnectionString"]!.GetValue<string>();
                Console.WriteLine($"[DEBUG] Postgres connection string: {pgConn}");
                Console.WriteLine("[DEBUG] Calling ImportToPostgres...");
                await ImportToPostgres(seedData, pgConn);
                Console.WriteLine("[DEBUG] Imported to Postgres");

                Console.WriteLine("[DEBUG] Calling ImportToCosmosDb...");
                await ImportToCosmosDb(seedData, config["CosmosDb"]!.AsObject());
                Console.WriteLine("[DEBUG] Imported to CosmosDB");

                Console.WriteLine("Seed data imported to Postgres and CosmosDB.");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("[ERROR] " + ex);
                Environment.Exit(1);
            }
        }

        static async Task ImportToPostgres(JsonObject seed, string connStr)
        {
            Console.WriteLine("[DEBUG] ImportToPostgres: Opening connection...");
            using var conn = new NpgsqlConnection(connStr);
            await conn.OpenAsync();
            Console.WriteLine("[DEBUG] ImportToPostgres: Connection opened.");
            using var tx = conn.BeginTransaction();
            Console.WriteLine("[DEBUG] ImportToPostgres: Transaction started.");

            // AGENTS
            Console.WriteLine("[DEBUG] ImportToPostgres: Inserting agents...");
            foreach (var agentNode in seed["agents"]!.AsArray())
            {
                var agent = agentNode!.AsObject();
                await using var cmd = new NpgsqlCommand(@"
                    INSERT INTO agents (id, external_ids, name, email, roles, organization_id, status, created_at)
                    VALUES (@id, @external_ids, @name, @email, @roles, @organization_id, @status, @created_at)
                    ON CONFLICT (id) DO NOTHING;", conn, tx);
                cmd.Parameters.AddWithValue("id", agent["id"]!.GetValue<string>());
                cmd.Parameters.AddWithValue("external_ids", agent["externalIds"]!.ToJsonString());
                cmd.Parameters.AddWithValue("name", agent["name"]!.GetValue<string>());
                cmd.Parameters.AddWithValue("email", agent["email"]!.GetValue<string>());
                cmd.Parameters.AddWithValue("roles", agent["roles"]!.Deserialize<string[]>()!);
                cmd.Parameters.AddWithValue("organization_id", agent["organizationId"] != null ? agent["organizationId"]!.GetValue<string>() : (object)DBNull.Value);
                cmd.Parameters.AddWithValue("status", agent["status"]!.GetValue<string>());
                cmd.Parameters.AddWithValue("created_at", DateTime.Parse(agent["createdAt"]!.GetValue<string>()));
                await cmd.ExecuteNonQueryAsync();
            }

            Console.WriteLine("[DEBUG] ImportToPostgres: Agents inserted.");

            // CONTACTS
            Console.WriteLine("[DEBUG] ImportToPostgres: Inserting contacts...");
            foreach (var contactNode in seed["contacts"]!.AsArray())
            {
                var contact = contactNode!.AsObject();
                await using var cmd = new NpgsqlCommand(@"
                    INSERT INTO contacts (id, external_ids, first_name, last_name, emails, phones, address, tags, lead_source, owner_id, created_at, updated_at, custom_fields, privacy_level)
                    VALUES (@id, @external_ids, @first_name, @last_name, @emails, @phones, @address, @tags, @lead_source, @owner_id, @created_at, @updated_at, @custom_fields, @privacy_level)
                    ON CONFLICT (id) DO NOTHING;", conn, tx);
                cmd.Parameters.AddWithValue("id", contact["id"]!.GetValue<string>());
                cmd.Parameters.AddWithValue("external_ids", contact["externalIds"]!.ToJsonString());
                cmd.Parameters.AddWithValue("first_name", contact["firstName"]!.GetValue<string>());
                cmd.Parameters.AddWithValue("last_name", contact["lastName"]!.GetValue<string>());
                cmd.Parameters.AddWithValue("emails", contact["emails"]!.ToJsonString());
                cmd.Parameters.AddWithValue("phones", contact["phones"]!.ToJsonString());
                cmd.Parameters.AddWithValue("address", contact["address"]!.ToJsonString());
                cmd.Parameters.AddWithValue("tags", contact["tags"]!.Deserialize<string[]>()!);
                cmd.Parameters.AddWithValue("lead_source", contact["leadSource"] != null ? contact["leadSource"]!.GetValue<string>() : (object)DBNull.Value);
                cmd.Parameters.AddWithValue("owner_id", contact["ownerId"]!.GetValue<string>());
                cmd.Parameters.AddWithValue("created_at", DateTime.Parse(contact["createdAt"]!.GetValue<string>()));
                cmd.Parameters.AddWithValue("updated_at", contact["updatedAt"] != null ? DateTime.Parse(contact["updatedAt"]!.GetValue<string>()) : (object)DBNull.Value);
                cmd.Parameters.AddWithValue("custom_fields", contact["customFields"]!.ToJsonString());
                cmd.Parameters.AddWithValue("privacy_level", contact["privacyLevel"] != null ? contact["privacyLevel"]!.GetValue<string>() : "public");
                await cmd.ExecuteNonQueryAsync();
            }

            Console.WriteLine("[DEBUG] ImportToPostgres: Contacts inserted.");

            // INTERACTIONS
            Console.WriteLine("[DEBUG] ImportToPostgres: Inserting interactions...");
            foreach (var interactionNode in seed["interactions"]!.AsArray())
            {
                var interaction = interactionNode!.AsObject();
                await using var cmd = new NpgsqlCommand(@"
                    INSERT INTO interactions (id, contact_id, external_ids, type, direction, timestamp, agent_id, content, channel, status, related_entities, tags, custom_fields)
                    VALUES (@id, @contact_id, @external_ids, @type, @direction, @timestamp, @agent_id, @content, @channel, @status, @related_entities, @tags, @custom_fields)
                    ON CONFLICT (id) DO NOTHING;", conn, tx);
                cmd.Parameters.AddWithValue("id", interaction["id"]!.GetValue<string>());
                cmd.Parameters.AddWithValue("contact_id", interaction["contactId"]!.GetValue<string>());
                cmd.Parameters.AddWithValue("external_ids", interaction["externalIds"]!.ToJsonString());
                cmd.Parameters.AddWithValue("type", interaction["type"]!.GetValue<string>());
                cmd.Parameters.AddWithValue("direction", interaction["direction"]!.GetValue<string>());
                cmd.Parameters.AddWithValue("timestamp", DateTime.Parse(interaction["timestamp"]!.GetValue<string>()));
                cmd.Parameters.AddWithValue("agent_id", interaction["agentId"] != null ? interaction["agentId"]!.GetValue<string>() : (object)DBNull.Value);
                cmd.Parameters.AddWithValue("content", interaction["content"] != null ? interaction["content"]!.GetValue<string>() : (object)DBNull.Value);
                cmd.Parameters.AddWithValue("channel", interaction["channel"] != null ? interaction["channel"]!.GetValue<string>() : (object)DBNull.Value);
                cmd.Parameters.AddWithValue("status", interaction["status"] != null ? interaction["status"]!.GetValue<string>() : (object)DBNull.Value);
                cmd.Parameters.AddWithValue("related_entities", interaction["relatedEntities"]!.ToJsonString());
                cmd.Parameters.AddWithValue("tags", interaction["tags"]!.Deserialize<string[]>()!);
                cmd.Parameters.AddWithValue("custom_fields", interaction["customFields"]!.ToJsonString());
                await cmd.ExecuteNonQueryAsync();
            }

            Console.WriteLine("[DEBUG] ImportToPostgres: Interactions inserted.");

            await tx.CommitAsync();
            Console.WriteLine("[DEBUG] ImportToPostgres: Transaction committed.");
        }

        static async Task ImportToCosmosDb(JsonObject seed, JsonObject cosmosConfig)
        {
            Console.WriteLine("[DEBUG] ImportToCosmosDb: Starting...");
            var endpoint = cosmosConfig["AccountEndpoint"]!.GetValue<string>();
            var key = cosmosConfig["AccountKey"]!.GetValue<string>();
            var dbName = cosmosConfig["DatabaseName"]!.GetValue<string>();
            var containers = cosmosConfig["Containers"]!.AsObject();
            Console.WriteLine($"[DEBUG] ImportToCosmosDb: Connecting to Cosmos endpoint {endpoint}...");
            var client = new CosmosClient(endpoint, key);
            var dbResponse = await client.CreateDatabaseIfNotExistsAsync(dbName);
            var db = dbResponse.Database;
            Console.WriteLine($"[DEBUG] ImportToCosmosDb: Connected to database {dbName}.");
            foreach (var entity in new[] { "agents", "contacts", "interactions" })
            {
                Console.WriteLine($"[DEBUG] ImportToCosmosDb: Processing entity {entity}...");
                var containerName = containers[entity]!.GetValue<string>();
                var containerResponse = await db.CreateContainerIfNotExistsAsync(containerName, "/id");
                var container = containerResponse.Container;
                Console.WriteLine($"[DEBUG] ImportToCosmosDb: Upserting items into container {containerName}...");
                foreach (var item in seed[entity]!.AsArray())
                {
                    await container.UpsertItemAsync(item.Deserialize<object>(), new Microsoft.Azure.Cosmos.PartitionKey(item["id"]!.GetValue<string>()));
                }
                Console.WriteLine($"[DEBUG] ImportToCosmosDb: Finished upserting {entity}.");
            }
            Console.WriteLine("[DEBUG] ImportToCosmosDb: All entities processed.");
        }
    }
}
