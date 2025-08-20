using System.IO;
using System.Text.Json.Nodes;
using System.Text.Json;
using Json.Schema;
using Xunit;

namespace Emma.Schemas.Tests;

public class SchemaValidationTests
{
    private static string SchemasDir => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "schemas"));

    private static JsonSchema LoadSchema(string name)
    {
        var path = Path.Combine(SchemasDir, name);
        Assert.True(File.Exists(path), $"Schema file not found: {path}");
        var schemaText = File.ReadAllText(path);
        // Remove explicit $schema draft declaration to use library defaults
        try
        {
            var node = JsonNode.Parse(schemaText) as JsonObject;
            if (node != null && node.ContainsKey("$schema"))
            {
                node.Remove("$schema");
                schemaText = node.ToJsonString(new JsonSerializerOptions { WriteIndented = false });
            }
        }
        catch
        {
            // If parsing fails, fall back to original text
        }
        var schema = JsonSchema.FromText(schemaText);
        return schema;
    }

    private static void AssertValid(JsonSchema schema, JsonNode node)
    {
        var result = schema.Evaluate(node);
        Assert.True(result.IsValid);
    }

    private static void AssertInvalid(JsonSchema schema, JsonNode node)
    {
        var result = schema.Evaluate(node);
        Assert.False(result.IsValid);
    }

    [Trait("Category", "Schema")]
    [Fact]
    public void ActionValidationResult_Valid_Passes()
    {
        var schema = LoadSchema("ActionValidationResult.schema.json");
        var sample = new JsonObject
        {
            ["decision"] = "NeedsApproval",
            ["reason"] = new JsonObject { ["code"] = "RISK_HIGH", ["message"] = "High risk action" },
            ["alternatives"] = new JsonArray
            {
                new JsonObject { ["title"] = "Send email instead", ["description"] = "Less risky channel" }
            },
            ["overrideMode"] = "RiskBased",
            ["userOverrides"] = new JsonObject { ["note"] = "user prefers email" },
            ["traceId"] = "abcd-1234-efgh-5678",
            ["tenantId"] = "tenant-xyz",
            ["orgId"] = "org-123",
            ["modelName"] = "gpt-4o",
            ["modelVersion"] = "2024-05-xx",
            ["tokensIn"] = 123,
            ["tokensOut"] = 45,
            ["totalCostEstimate"] = 0.0123,
            ["aiConfidenceScore"] = 0.76,
            ["durationMs"] = 512
        };
        AssertValid(schema, sample);
    }

    [Trait("Category", "Schema")]
    [Fact]
    public void ActionValidationResult_Invalid_Fails()
    {
        var schema = LoadSchema("ActionValidationResult.schema.json");
        var sample = new JsonObject
        {
            ["decision"] = "INVALID",
            ["reason"] = new JsonObject { ["message"] = "missing code" },
            ["traceId"] = "t",
            ["tenantId"] = "",
            ["orgId"] = "",
            ["aiConfidenceScore"] = 1.5,
            ["durationMs"] = -1
        };
        AssertInvalid(schema, sample);
    }

    [Trait("Category", "Schema")]
    [Fact]
    public void UserApprovalRequest_Valid_Passes()
    {
        var schema = LoadSchema("UserApprovalRequest.schema.json");
        var sample = new JsonObject
        {
            ["traceId"] = "abcd-1234-efgh-5678",
            ["tenantId"] = "tenant-xyz",
            ["orgId"] = "org-123",
            ["actionSummary"] = "Send message to client with meeting details",
            ["riskLevel"] = "Medium",
            ["proposedAction"] = new JsonObject
            {
                ["type"] = "SendMessage",
                ["parameters"] = new JsonObject { ["channel"] = "email", ["to"] = "contact-123" }
            },
            ["alternatives"] = new JsonArray { new JsonObject { ["title"] = "Send SMS", ["description"] = "Short notification" } },
            ["overrideMode"] = "AlwaysAsk",
            ["userOverrides"] = new JsonObject { ["locale"] = "en-US" },
            ["aiConfidenceScore"] = 0.62,
            ["durationMs"] = 421
        };
        AssertValid(schema, sample);
    }

    [Trait("Category", "Schema")]
    [Fact]
    public void UserApprovalRequest_Invalid_Fails()
    {
        var schema = LoadSchema("UserApprovalRequest.schema.json");
        var sample = new JsonObject
        {
            ["actionSummary"] = "",
            ["riskLevel"] = "Unknown",
            ["proposedAction"] = new JsonObject { ["parameters"] = new JsonObject() },
            ["alternatives"] = new JsonArray()
        };
        AssertInvalid(schema, sample);
    }
}
