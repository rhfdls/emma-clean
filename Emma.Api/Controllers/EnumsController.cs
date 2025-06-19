using Emma.Core.Interfaces;
using Emma.Core.Models;
using Emma.Core.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace Emma.Api.Controllers;

/// <summary>
/// API controller for dynamic enum management
/// Provides endpoints for business users to access and validate enum values
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class EnumsController : ControllerBase
{
    private readonly IEnumProvider _enumProvider;
    private readonly ILogger<EnumsController> _logger;

    public EnumsController(IEnumProvider enumProvider, ILogger<EnumsController> logger)
    {
        _enumProvider = enumProvider;
        _logger = logger;
    }

    /// <summary>
    /// Get all available enum types for the specified context
    /// </summary>
    /// <param name="industryCode">Optional industry code for filtering</param>
    /// <param name="agentType">Optional agent type for filtering</param>
    /// <param name="tenantId">Optional tenant ID for filtering</param>
    /// <returns>List of available enum types</returns>
    [HttpGet("types")]
    public async Task<ActionResult<IEnumerable<string>>> GetEnumTypes(
        [FromQuery] string? industryCode = null,
        [FromQuery] string? agentType = null,
        [FromQuery] string? tenantId = null)
    {
        try
        {
            var context = EnumExtensions.CreateContext(industryCode, agentType, tenantId);
            var enumTypes = await _enumProvider.GetAvailableEnumTypesAsync(context);
            
            _logger.LogDebug("Retrieved {Count} enum types for context: Industry={Industry}, Agent={Agent}", 
                enumTypes.Count(), industryCode, agentType);
            
            return Ok(enumTypes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving enum types");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get enum values for a specific enum type
    /// </summary>
    /// <param name="enumType">The enum type to retrieve</param>
    /// <param name="industryCode">Optional industry code for overrides</param>
    /// <param name="agentType">Optional agent type for overrides</param>
    /// <param name="tenantId">Optional tenant ID for context</param>
    /// <param name="format">Response format: 'full', 'dropdown', 'api' (default: 'full')</param>
    /// <returns>Enum values in the specified format</returns>
    [HttpGet("{enumType}")]
    public async Task<ActionResult> GetEnumValues(
        string enumType,
        [FromQuery] string? industryCode = null,
        [FromQuery] string? agentType = null,
        [FromQuery] string? tenantId = null,
        [FromQuery] string format = "full")
    {
        try
        {
            var context = EnumExtensions.CreateContext(industryCode, agentType, tenantId);
            
            switch (format.ToLowerInvariant())
            {
                case "dropdown":
                    var dropdown = _enumProvider.GetEnumDropdown(enumType, context);
                    return Ok(dropdown);
                    
                case "api":
                    var apiFormat = await _enumProvider.ToApiFormatAsync(enumType, context);
                    return Ok(apiFormat);
                    
                case "full":
                default:
                    var values = await _enumProvider.GetEnumValuesAsync(enumType, context);
                    return Ok(values);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving enum values for {EnumType}", enumType);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get a specific enum value
    /// </summary>
    /// <param name="enumType">The enum type</param>
    /// <param name="key">The enum value key</param>
    /// <param name="industryCode">Optional industry code for overrides</param>
    /// <param name="agentType">Optional agent type for overrides</param>
    /// <param name="tenantId">Optional tenant ID for context</param>
    /// <returns>The specific enum value or 404 if not found</returns>
    [HttpGet("{enumType}/{key}")]
    public async Task<ActionResult<EnumValue>> GetEnumValue(
        string enumType,
        string key,
        [FromQuery] string? industryCode = null,
        [FromQuery] string? agentType = null,
        [FromQuery] string? tenantId = null)
    {
        try
        {
            var context = EnumExtensions.CreateContext(industryCode, agentType, tenantId);
            var enumValue = _enumProvider.GetEnumValue(enumType, key, context);
            
            if (enumValue == null)
            {
                _logger.LogDebug("Enum value not found: {EnumType}.{Key}", enumType, key);
                return NotFound($"Enum value '{key}' not found in '{enumType}'");
            }
            
            return Ok(enumValue);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving enum value {EnumType}.{Key}", enumType, key);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Validate enum values
    /// </summary>
    /// <param name="enumType">The enum type</param>
    /// <param name="keys">Array of keys to validate</param>
    /// <param name="industryCode">Optional industry code for overrides</param>
    /// <param name="agentType">Optional agent type for overrides</param>
    /// <param name="tenantId">Optional tenant ID for context</param>
    /// <returns>Validation results for each key</returns>
    [HttpPost("{enumType}/validate")]
    public async Task<ActionResult<Dictionary<string, bool>>> ValidateEnumValues(
        string enumType,
        [FromBody] string[] keys,
        [FromQuery] string? industryCode = null,
        [FromQuery] string? agentType = null,
        [FromQuery] string? tenantId = null)
    {
        try
        {
            var context = EnumExtensions.CreateContext(industryCode, agentType, tenantId);
            var results = await _enumProvider.ValidateMultipleAsync(enumType, keys, context);
            
            _logger.LogDebug("Validated {Count} enum values for {EnumType}", keys.Length, enumType);
            return Ok(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating enum values for {EnumType}", enumType);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get enum metadata
    /// </summary>
    /// <param name="enumType">The enum type</param>
    /// <param name="industryCode">Optional industry code for overrides</param>
    /// <param name="agentType">Optional agent type for overrides</param>
    /// <param name="tenantId">Optional tenant ID for context</param>
    /// <returns>Enum metadata including counts and configuration</returns>
    [HttpGet("{enumType}/metadata")]
    public async Task<ActionResult> GetEnumMetadata(
        string enumType,
        [FromQuery] string? industryCode = null,
        [FromQuery] string? agentType = null,
        [FromQuery] string? tenantId = null)
    {
        try
        {
            var context = EnumExtensions.CreateContext(industryCode, agentType, tenantId);
            var metadata = await _enumProvider.GetEnumMetadataAsync(enumType, context);
            return Ok(metadata);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving enum metadata for {EnumType}", enumType);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Search enum values
    /// </summary>
    /// <param name="enumType">The enum type</param>
    /// <param name="searchTerm">Search term to filter by</param>
    /// <param name="industryCode">Optional industry code for overrides</param>
    /// <param name="agentType">Optional agent type for overrides</param>
    /// <param name="tenantId">Optional tenant ID for context</param>
    /// <returns>Filtered enum values matching the search term</returns>
    [HttpGet("{enumType}/search")]
    public async Task<ActionResult<IEnumerable<EnumValue>>> SearchEnumValues(
        string enumType,
        [FromQuery] string searchTerm,
        [FromQuery] string? industryCode = null,
        [FromQuery] string? agentType = null,
        [FromQuery] string? tenantId = null)
    {
        try
        {
            var context = EnumExtensions.CreateContext(industryCode, agentType, tenantId);
            var results = await _enumProvider.SearchAsync(enumType, searchTerm, context);
            
            _logger.LogDebug("Search for '{SearchTerm}' in {EnumType} returned {Count} results", 
                searchTerm, enumType, results.Count());
            
            return Ok(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching enum values for {EnumType}", enumType);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get enum values grouped by category
    /// </summary>
    /// <param name="enumType">The enum type</param>
    /// <param name="industryCode">Optional industry code for overrides</param>
    /// <param name="agentType">Optional agent type for overrides</param>
    /// <param name="tenantId">Optional tenant ID for context</param>
    /// <returns>Enum values grouped by category</returns>
    [HttpGet("{enumType}/grouped")]
    public async Task<ActionResult<Dictionary<string, IEnumerable<EnumValue>>>> GetGroupedEnumValues(
        string enumType,
        [FromQuery] string? industryCode = null,
        [FromQuery] string? agentType = null,
        [FromQuery] string? tenantId = null)
    {
        try
        {
            var context = EnumExtensions.CreateContext(industryCode, agentType, tenantId);
            var grouped = await _enumProvider.GetGroupedByCategoryAsync(enumType, context);
            
            return Ok(grouped);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving grouped enum values for {EnumType}", enumType);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Reload enum configuration (for development/admin use)
    /// </summary>
    /// <returns>Success message</returns>
    [HttpPost("reload")]
    public async Task<ActionResult> ReloadConfiguration()
    {
        try
        {
            await _enumProvider.ReloadConfigurationAsync();
            _logger.LogInformation("Enum configuration reloaded via API");
            
            return Ok(new { message = "Enum configuration reloaded successfully", timestamp = DateTime.UtcNow });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reloading enum configuration");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get configuration metadata including version information
    /// </summary>
    /// <returns>Configuration metadata</returns>
    [HttpGet("metadata")]
    public async Task<ActionResult<EnumConfigurationMetadata>> GetConfigurationMetadata()
    {
        try
        {
            Console.WriteLine($"[IEnumProvider] {typeof(EnumConfigurationMetadata).AssemblyQualifiedName}");
            var metadata = _enumProvider.GetConfigurationMetadata();
            return Ok(metadata);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving configuration metadata");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Create a new version backup of the current configuration
    /// </summary>
    /// <param name="request">Version creation request</param>
    /// <returns>Created version identifier</returns>
    [HttpPost("versions")]
    public async Task<ActionResult<string>> CreateVersion([FromBody] CreateVersionRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Description))
            {
                return BadRequest("Description is required");
            }

            if (string.IsNullOrWhiteSpace(request.CreatedBy))
            {
                return BadRequest("CreatedBy is required");
            }

            var version = _enumProvider.CreateVersion(request.Description, request.CreatedBy, request.Tags);
            
            _logger.LogInformation("Created enum configuration version {Version} via API by {User}", 
                version, request.CreatedBy);
            
            return Ok(new { version, message = "Version created successfully", timestamp = DateTime.UtcNow });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating version");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get version history
    /// </summary>
    /// <returns>List of version history entries</returns>
    [HttpGet("versions")]
    public ActionResult<IEnumerable<VersionHistoryEntry>> GetVersionHistory()
    {
        try
        {
            var versions = _enumProvider.GetVersionHistory();
            return Ok(versions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving version history");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Rollback to a specific version
    /// </summary>
    /// <param name="version">Version to rollback to</param>
    /// <param name="request">Rollback request details</param>
    /// <returns>Rollback result</returns>
    [HttpPost("versions/{version}/rollback")]
    public async Task<ActionResult> RollbackToVersion(string version, [FromBody] RollbackRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.RolledBackBy))
            {
                return BadRequest("RolledBackBy is required");
            }

            if (string.IsNullOrWhiteSpace(request.Reason))
            {
                return BadRequest("Reason is required");
            }

            var success = _enumProvider.RollbackToVersion(version, request.RolledBackBy, request.Reason);
            
            if (success)
            {
                _logger.LogInformation("Rolled back to version {Version} via API by {User}: {Reason}", 
                    version, request.RolledBackBy, request.Reason);
                
                return Ok(new { 
                    message = $"Successfully rolled back to version {version}", 
                    version, 
                    timestamp = DateTime.UtcNow 
                });
            }
            else
            {
                return BadRequest($"Failed to rollback to version {version}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rolling back to version {Version}", version);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Compare two versions
    /// </summary>
    /// <param name="fromVersion">Source version</param>
    /// <param name="toVersion">Target version</param>
    /// <returns>Version comparison result</returns>
    [HttpGet("versions/compare")]
    public async Task<ActionResult<VersionComparisonResult>> CompareVersions(
        [FromQuery] string fromVersion, 
        [FromQuery] string toVersion)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(fromVersion) || string.IsNullOrWhiteSpace(toVersion))
            {
                return BadRequest("Both fromVersion and toVersion are required");
            }

            var comparison = await _enumProvider.CompareVersionsAsync(fromVersion, toVersion);
            return Ok(comparison);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error comparing versions {FromVersion} and {ToVersion}", fromVersion, toVersion);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get change log entries with optional filtering
    /// </summary>
    /// <param name="enumType">Optional filter by enum type</param>
    /// <param name="fromDate">Optional start date filter</param>
    /// <param name="toDate">Optional end date filter</param>
    /// <param name="changedBy">Optional filter by user</param>
    /// <returns>List of change log entries</returns>
    [HttpGet("changelog")]
    public async Task<ActionResult<IEnumerable<ChangeLogEntry>>> GetChangeLog(
        [FromQuery] string? enumType = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] string? changedBy = null)
    {
        try
        {
            var changeLogs = await _enumProvider.GetChangeLogAsync(enumType, fromDate, toDate, changedBy);
            return Ok(changeLogs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving change log entries");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Submit configuration changes for approval
    /// </summary>
    /// <param name="request">Approval submission request</param>
    /// <returns>Approval request ID</returns>
    [HttpPost("approvals")]
    public async Task<ActionResult<string>> SubmitForApproval([FromBody] SubmitApprovalRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.SubmittedBy))
            {
                return BadRequest("SubmittedBy is required");
            }

            if (string.IsNullOrWhiteSpace(request.Description))
            {
                return BadRequest("Description is required");
            }

            if (request.RequiredApprovers == null || !request.RequiredApprovers.Any())
            {
                return BadRequest("At least one required approver must be specified");
            }

            var approvalId = await _enumProvider.SubmitForApprovalAsync(
                request.SubmittedBy, 
                request.Description, 
                request.RequiredApprovers);
            
            _logger.LogInformation("Submitted enum configuration for approval via API by {User}: {Description}", 
                request.SubmittedBy, request.Description);
            
            return Ok(new { 
                approvalId, 
                message = "Configuration submitted for approval", 
                timestamp = DateTime.UtcNow 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting for approval");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Process an approval request (approve or reject)
    /// </summary>
    /// <param name="approvalId">Approval request ID</param>
    /// <param name="request">Approval processing request</param>
    /// <returns>Processing result</returns>
    [HttpPost("approvals/{approvalId}/process")]
    public async Task<ActionResult> ProcessApproval(string approvalId, [FromBody] ProcessApprovalRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Approver))
            {
                return BadRequest("Approver is required");
            }

            var success = await _enumProvider.ProcessApprovalAsync(
                approvalId, 
                request.Approver, 
                request.Approved, 
                request.Comments);
            
            if (success)
            {
                _logger.LogInformation("Processed approval {ApprovalId} via API by {Approver}: {Action}", 
                    approvalId, request.Approver, request.Approved ? "Approved" : "Rejected");
                
                return Ok(new { 
                    message = $"Configuration {(request.Approved ? "approved" : "rejected")} successfully", 
                    approvalId, 
                    timestamp = DateTime.UtcNow 
                });
            }
            else
            {
                return BadRequest($"Failed to process approval {approvalId}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing approval {ApprovalId}", approvalId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Export configuration to file
    /// </summary>
    /// <param name="includeHistory">Whether to include version history</param>
    /// <returns>Configuration file content</returns>
    [HttpGet("export")]
    public async Task<ActionResult> ExportConfiguration([FromQuery] bool includeHistory = false)
    {
        try
        {
            var tempFilePath = Path.GetTempFileName();
            
            var success = await _enumProvider.ExportConfigurationAsync(tempFilePath, includeHistory);
            
            if (success)
            {
                var content = await System.IO.File.ReadAllBytesAsync(tempFilePath);
                System.IO.File.Delete(tempFilePath);
                
                var fileName = $"enum-config-{DateTime.UtcNow:yyyyMMdd-HHmmss}.json";
                
                return File(content, "application/json", fileName);
            }
            else
            {
                return StatusCode(500, "Failed to export configuration");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting configuration");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Import configuration from uploaded file
    /// </summary>
    /// <param name="file">Configuration file to import</param>
    /// <param name="importedBy">User performing the import</param>
    /// <param name="mergeStrategy">How to handle conflicts</param>
    /// <returns>Import result</returns>
    [HttpPost("import")]
    public ActionResult<ImportResult> ImportConfiguration(
        IFormFile file,
        [FromForm] string importedBy,
        [FromForm] PromptMergeStrategy mergeStrategy = PromptMergeStrategy.Replace)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded");
            }

            if (string.IsNullOrWhiteSpace(importedBy))
            {
                return BadRequest("ImportedBy is required");
            }

            if (!file.FileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest("Only JSON files are supported");
            }

            var tempFilePath = Path.GetTempFileName();
            
            try
            {
                using (var stream = new FileStream(tempFilePath, FileMode.Create))
                {
                    file.CopyTo(stream);
                }

                var result = new ImportResult();
                result.Success = _enumProvider.ImportConfiguration(tempFilePath, importedBy, mergeStrategy);
                
                _logger.LogInformation("Imported enum configuration via API by {User}: {FileName} (Success: {Success})", 
                    importedBy, file.FileName, result.Success);
                
                return Ok(result);
            }
            finally
            {
                if (System.IO.File.Exists(tempFilePath))
                {
                    System.IO.File.Delete(tempFilePath);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing configuration");
            return StatusCode(500, "Internal server error");
        }
    }
}

// Request/Response Models for Versioning API

/// <summary>
/// Request model for creating a version
/// </summary>
public class CreateVersionRequest
{
    public string Description { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
    public List<string>? Tags { get; set; }
}

/// <summary>
/// Request model for rollback operation
/// </summary>
public class RollbackRequest
{
    public string RolledBackBy { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// Request model for approval submission
/// </summary>
public class SubmitApprovalRequest
{
    public string SubmittedBy { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> RequiredApprovers { get; set; } = new();
}

/// <summary>
/// Request model for processing approval
/// </summary>
public class ProcessApprovalRequest
{
    public string Approver { get; set; } = string.Empty;
    public bool Approved { get; set; }
    public string? Comments { get; set; }
}
