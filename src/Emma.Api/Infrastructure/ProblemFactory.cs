using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Emma.Api.Infrastructure
{
    public static class ProblemFactory
    {
        // Stable problem type constants
        public const string ValidationError = "/problems/validation-error";
        public const string OrgMismatch = "/problems/org-mismatch";
        public const string NotFound = "/problems/not-found";
        public const string DevOnly = "/problems/dev-only";
        public const string DependencyUnhealthy = "/problems/dependency-unhealthy";
        public const string Unauthorized = "/problems/unauthorized";
        public const string Forbidden = "/problems/forbidden";
        public const string Conflict = "/problems/conflict";
        public const string Unprocessable = "/problems/unprocessable";
        public const string InternalError = "/problems/internal-error";

        public static ProblemDetails Create(HttpContext httpContext, int status, string title, string detail, string type)
        {
            var problem = new ProblemDetails
            {
                Status = status,
                Title = title,
                Detail = detail,
                Type = type,
                Instance = httpContext?.Request?.Path.Value
            };
            var traceId = Activity.Current?.Id ?? httpContext?.TraceIdentifier;
            if (!string.IsNullOrEmpty(traceId))
            {
                problem.Extensions["traceId"] = traceId!;
            }
            return problem;
        }

        public static IActionResult ToResult(this ProblemDetails problem)
        {
            return new ObjectResult(problem) { StatusCode = problem.Status };
        }
    }
}
