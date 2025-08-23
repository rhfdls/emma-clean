using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Emma.Api.Infrastructure.Swagger
{
    public class ProblemDetailsResponsesOperationFilter : IOperationFilter
    {
        private static OpenApiObject Example(int status, string title, string type, string instance)
        {
            return new OpenApiObject
            {
                ["type"] = new OpenApiString(type),
                ["title"] = new OpenApiString(title),
                ["status"] = new OpenApiInteger(status),
                ["detail"] = new OpenApiString("Example error detail."),
                ["instance"] = new OpenApiString(instance),
                ["traceId"] = new OpenApiString("00000000000000000000000000000000")
            };
        }

        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            // Ensure ProblemDetails schema is available
            var pdSchema = context.SchemaGenerator.GenerateSchema(typeof(ProblemDetails), context.SchemaRepository);

            void AddProblemResponse(string code, int status, string title, string type, string instance)
            {
                operation.Responses[code] = new OpenApiResponse
                {
                    Description = title,
                    Content = new Dictionary<string, OpenApiMediaType>
                    {
                        ["application/problem+json"] = new OpenApiMediaType
                        {
                            Schema = pdSchema,
                            Examples = new Dictionary<string, OpenApiExample>
                            {
                                [code] = new OpenApiExample { Value = Example(status, title, type, instance) }
                            }
                        }
                    }
                };
            }

            // Standard problem responses
            AddProblemResponse("400", 400, "Validation failed", "/problems/validation-error", "/example/path");
            AddProblemResponse("403", 403, "Forbidden", "/problems/forbidden", "/example/path");
            AddProblemResponse("404", 404, "Not Found", "/problems/not-found", "/example/path");
            AddProblemResponse("409", 409, "Conflict", "/problems/conflict", "/example/path");
            AddProblemResponse("422", 422, "Unprocessable Entity", "/problems/unprocessable", "/example/path");
            AddProblemResponse("503", 503, "Service Unavailable", "/problems/dependency-unhealthy", "/example/path");
        }
    }
}
