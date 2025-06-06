using Emma.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace Emma.Api.Attributes;

/// <summary>
/// Authorization attribute that enforces contact access permissions.
/// Ensures agents can only access contacts they own or have collaboration access to.
/// </summary>
public class RequireContactAccessAttribute : ActionFilterAttribute
{
    private readonly string _contactIdParameterName;
    
    public RequireContactAccessAttribute(string contactIdParameterName = "contactId")
    {
        _contactIdParameterName = contactIdParameterName;
    }
    
    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var contactAccessService = context.HttpContext.RequestServices.GetRequiredService<IContactAccessService>();
        
        // Get the requesting agent ID from claims
        var agentIdClaim = context.HttpContext.User.FindFirst("AgentId")?.Value;
        if (!Guid.TryParse(agentIdClaim, out var requestingAgentId))
        {
            context.Result = new UnauthorizedObjectResult("Invalid or missing agent ID");
            return;
        }
        
        // Get the contact ID from route parameters
        if (!context.ActionArguments.TryGetValue(_contactIdParameterName, out var contactIdObj) ||
            !Guid.TryParse(contactIdObj?.ToString(), out var contactId))
        {
            context.Result = new BadRequestObjectResult($"Invalid or missing {_contactIdParameterName}");
            return;
        }
        
        // Check access permissions
        var canAccess = await contactAccessService.CanAccessContactAsync(contactId, requestingAgentId);
        if (!canAccess)
        {
            // Log the access denial for audit purposes
            await contactAccessService.LogContactAccessAsync(
                contactId, 
                requestingAgentId, 
                false, 
                "Access denied - insufficient permissions");
                
            context.Result = new ForbidResult();
            return;
        }
        
        // Log successful access
        await contactAccessService.LogContactAccessAsync(
            contactId, 
            requestingAgentId, 
            true, 
            "Access granted");
        
        await next();
    }
}
