using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Emma.Api.Infrastructure
{
    // Optional helper to standardize ProblemDetails shapes for field-level forbidden errors
    public static class ApiErrors
    {
        public static IActionResult ForbiddenFields(ControllerBase c, string detail, IEnumerable<string> blocked)
        {
            var problem = ProblemFactory.Create(
                c.HttpContext!,
                StatusCodes.Status403Forbidden,
                title: "Forbidden",
                detail: detail,
                type: "https://emma.ai/errors/field_update_forbidden"
            );
            problem.Extensions["errors"] = new Dictionary<string, object?>
            {
                ["blockedFields"] = blocked?.ToArray() ?? Array.Empty<string>()
            };
            return problem.ToResult();
        }
    }
}
