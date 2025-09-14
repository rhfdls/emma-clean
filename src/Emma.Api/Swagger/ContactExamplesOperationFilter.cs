using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Emma.Api.Swagger
{
    public sealed class ContactExamplesOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation op, OperationFilterContext ctx)
        {
            var path = (ctx.ApiDescription.RelativePath ?? string.Empty).ToLowerInvariant();
            var method = (ctx.ApiDescription.HttpMethod ?? string.Empty).ToUpperInvariant();

            static IOpenApiAny AnyFrom(object o) => OpenApiAnyFactory.CreateFromJson(System.Text.Json.JsonSerializer.Serialize(o));

            // List examples
            if (path == "api/contacts" && method == "GET")
            {
                if (op.Parameters != null)
                {
                    SetExample(op.Parameters, "ownerId", "11111111-1111-1111-1111-111111111111");
                    SetExample(op.Parameters, "relationshipState", "Client");
                    SetExample(op.Parameters, "q", "smith");
                    SetExample(op.Parameters, "page", 1);
                    SetExample(op.Parameters, "size", 25);
                    SetExample(op.Parameters, "includeArchived", false);
                }

                if (op.Responses.TryGetValue("200", out var ok))
                {
                    ok.Content ??= new Dictionary<string, OpenApiMediaType>();
                    ok.Content["application/json"] = new OpenApiMediaType
                    {
                        Schema = ok.Content.TryGetValue("application/json", out var s) ? s.Schema : null,
                        Examples = new Dictionary<string, OpenApiExample>
                        {
                            ["defaultPage"] = new()
                            {
                                Value = AnyFrom(new [] {
                                    new { id="c1", firstName="Anna", lastName="Smith", ownerId="1111...", relationshipState="Client", isArchived=false },
                                    new { id="c2", firstName="Bob", lastName="Stone", ownerId="1111...", relationshipState="Prospect", isArchived=false }
                                })
                            }
                        }
                    };
                }
            }

            // Archive
            if (path == "api/contacts/{id}/archive" && method == "PATCH")
            {
                AddProblemExample(op, "403", new {
                    type="https://emma.ai/problems/forbidden",
                    title="Forbidden",
                    status=403,
                    detail="Only OrgOwner/Admin may archive contacts.",
                    traceId="abc123"
                });
                AddProblemExample(op, "409", new {
                    type="https://emma.ai/problems/conflict",
                    title="Conflict",
                    status=409,
                    detail="Contact is already archived.",
                    traceId="abc123"
                });
            }

            // Restore
            if (path == "api/contacts/{id}/restore" && method == "PATCH")
            {
                AddProblemExample(op, "403", new {
                    type="https://emma.ai/problems/forbidden",
                    title="Forbidden",
                    status=403,
                    detail="Only OrgOwner/Admin may restore contacts.",
                    traceId="abc123"
                });
                AddProblemExample(op, "409", new {
                    type="https://emma.ai/problems/conflict",
                    title="Contact is not archived",
                    status=409,
                    detail="Only archived contacts can be restored.",
                    traceId="abc123"
                });

                if (op.Responses.TryGetValue("200", out var ok))
                {
                    ok.Content ??= new Dictionary<string, OpenApiMediaType>();
                    ok.Content["application/json"] = new OpenApiMediaType
                    {
                        Examples = new Dictionary<string, OpenApiExample>
                        {
                            ["restored"] = new() { Value = AnyFrom(new { id="c1", firstName="Anna", lastName="Smith", isArchived=false }) }
                        }
                    };
                }
            }

            // Hard delete with reason
            if (path == "api/contacts/{id}" && method == "DELETE")
            {
                if (op.Parameters != null) SetExample(op.Parameters, "reason", "Data subject erasure request");

                AddProblemExample(op, "400", new {
                    type="https://emma.ai/problems/erase-confirmation-required",
                    title="Reason is required",
                    status=400,
                    detail="Provide a non-empty 'reason' to perform a hard delete.",
                    traceId="abc123"
                });
                AddProblemExample(op, "403", new {
                    type="https://emma.ai/problems/forbidden",
                    title="Forbidden",
                    status=403,
                    detail="Only OrgOwner/Admin may hard-delete contacts.",
                    traceId="abc123"
                });
            }

            static void SetExample(IList<OpenApiParameter> ps, string name, object example)
            {
                var p = ps.FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
                if (p != null) p.Example = OpenApiAnyFactory.CreateFromJson(System.Text.Json.JsonSerializer.Serialize(example));
            }

            static void AddProblemExample(OpenApiOperation op2, string code, object example)
            {
                if (!op2.Responses.TryGetValue(code, out var r)) return;
                r.Content ??= new Dictionary<string, OpenApiMediaType>();
                r.Content["application/problem+json"] = new OpenApiMediaType
                {
                    Examples = new Dictionary<string, OpenApiExample>
                    {
                        ["example"] = new() { Value = AnyFrom(example) }
                    }
                };
            }
        }
    }
}
