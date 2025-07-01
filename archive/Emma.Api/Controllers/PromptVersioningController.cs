using Emma.Core.Interfaces;
using Emma.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace Emma.Api.Controllers;

/// <summary>
/// API controller for prompt configuration versioning, rollback, and audit operations
/// Provides enterprise-grade configuration governance for prompt management
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class PromptVersioningController : ControllerBase
{
    private readonly IPromptProvider _promptProvider;
    private readonly ILogger<PromptVersioningController> _logger;

    public PromptVersioningController(IPromptProvider promptProvider, ILogger<PromptVersioningController> logger)
    {
        _promptProvider = promptProvider ?? throw new ArgumentNullException(nameof(promptProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Create a new version of the prompt configuration
    /// </summary>
    /// <param name="request">Version creation request containing description, creator, and optional tags</param>
    /// <returns>Version ID of the created backup</returns>
    [HttpPost("versions")]
    [ProducesResponseType(typeof(PromptConfigurationResponse), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 500)]
    public async Task<IActionResult> CreateVersion([FromBody] CreatePromptVersionRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Description))
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid Request",
                    Detail = "Version description is required",
                    Status = 400
                });
            }

            if (string.IsNullOrWhiteSpace(request.CreatedBy))
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid Request",
                    Detail = "CreatedBy field is required",
                    Status = 400
                });
            }

            var versionId = await _promptProvider.CreateVersionAsync(request.Description, request.CreatedBy, request.Tags);

            _logger.LogInformation("Created prompt configuration version {VersionId} by {User}", versionId, request.CreatedBy);

            return Ok(new PromptConfigurationResponse
            {
                Success = true,
                Data = versionId,
                Metadata = new Dictionary<string, object>
                {
                    ["versionId"] = versionId,
                    ["createdBy"] = request.CreatedBy,
                    ["description"] = request.Description,
                    ["tags"] = request.Tags ?? new List<string>()
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create prompt configuration version");
            return StatusCode(500, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "Failed to create prompt configuration version",
                Status = 500
            });
        }
    }

    /// <summary>
    /// Rollback prompt configuration to a specific version
    /// </summary>
    /// <param name="request">Rollback request containing version, user, and reason</param>
    /// <returns>Success status of the rollback operation</returns>
    [HttpPost("rollback")]
    [ProducesResponseType(typeof(PromptConfigurationResponse), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    [ProducesResponseType(typeof(ProblemDetails), 500)]
    public async Task<IActionResult> RollbackToVersion([FromBody] PromptRollbackRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Version))
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid Request",
                    Detail = "Version is required",
                    Status = 400
                });
            }

            if (string.IsNullOrWhiteSpace(request.RolledBackBy))
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid Request",
                    Detail = "RolledBackBy field is required",
                    Status = 400
                });
            }

            var success = await _promptProvider.RollbackToVersionAsync(request.Version, request.RolledBackBy, request.Reason);

            if (!success)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Version Not Found",
                    Detail = $"Version '{request.Version}' not found or rollback failed",
                    Status = 404
                });
            }

            _logger.LogInformation("Rolled back prompt configuration to version {Version} by {User}", 
                request.Version, request.RolledBackBy);

            return Ok(new PromptConfigurationResponse
            {
                Success = true,
                Data = request.Version,
                Metadata = new Dictionary<string, object>
                {
                    ["rolledBackToVersion"] = request.Version,
                    ["rolledBackBy"] = request.RolledBackBy,
                    ["reason"] = request.Reason ?? "No reason provided"
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to rollback prompt configuration to version {Version}", request.Version);
            return StatusCode(500, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "Failed to rollback prompt configuration",
                Status = 500
            });
        }
    }

    /// <summary>
    /// Get version history of prompt configuration changes
    /// </summary>
    /// <returns>List of version history entries</returns>
    [HttpGet("versions")]
    [ProducesResponseType(typeof(IEnumerable<PromptVersionHistoryEntry>), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 500)]
    public async Task<IActionResult> GetVersionHistory()
    {
        try
        {
            var history = await _promptProvider.GetVersionHistoryAsync();
            
            _logger.LogDebug("Retrieved {Count} prompt configuration versions", history.Count());
            
            return Ok(history);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve prompt configuration version history");
            return StatusCode(500, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "Failed to retrieve version history",
                Status = 500
            });
        }
    }

    /// <summary>
    /// Compare two versions of prompt configuration
    /// </summary>
    /// <param name="version1">First version to compare</param>
    /// <param name="version2">Second version to compare</param>
    /// <returns>Comparison result showing differences between versions</returns>
    [HttpGet("compare")]
    [ProducesResponseType(typeof(PromptVersionComparisonResult), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    [ProducesResponseType(typeof(ProblemDetails), 500)]
    public async Task<IActionResult> CompareVersions([FromQuery] string version1, [FromQuery] string version2)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(version1) || string.IsNullOrWhiteSpace(version2))
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid Request",
                    Detail = "Both version1 and version2 parameters are required",
                    Status = 400
                });
            }

            var comparison = await _promptProvider.CompareVersionsAsync(version1, version2);
            
            _logger.LogDebug("Compared prompt configuration versions {Version1} and {Version2}, found {DiffCount} differences", 
                version1, version2, comparison.Differences.Count);
            
            return Ok(comparison);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Version not found during comparison: {Version1} vs {Version2}", version1, version2);
            return NotFound(new ProblemDetails
            {
                Title = "Version Not Found",
                Detail = ex.Message,
                Status = 404
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to compare prompt configuration versions {Version1} and {Version2}", version1, version2);
            return StatusCode(500, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "Failed to compare versions",
                Status = 500
            });
        }
    }

    /// <summary>
    /// Get filtered audit log entries for prompt configuration changes
    /// </summary>
    /// <param name="fromDate">Start date for filtering (optional)</param>
    /// <param name="toDate">End date for filtering (optional)</param>
    /// <param name="agentType">Filter by agent type (optional)</param>
    /// <param name="changedBy">Filter by user who made changes (optional)</param>
    /// <param name="changeType">Filter by type of change (optional)</param>
    /// <returns>Filtered list of change log entries</returns>
    [HttpGet("audit-log")]
    [ProducesResponseType(typeof(IEnumerable<PromptChangeLogEntry>), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 500)]
    public async Task<IActionResult> GetAuditLog(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] string? agentType = null,
        [FromQuery] string? changedBy = null,
        [FromQuery] PromptChangeType? changeType = null)
    {
        try
        {
            var auditLog = await _promptProvider.GetChangeLogAsync(fromDate, toDate, agentType, changedBy, changeType);
            
            _logger.LogDebug("Retrieved {Count} prompt configuration audit log entries", auditLog.Count());
            
            return Ok(auditLog);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve prompt configuration audit log");
            return StatusCode(500, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "Failed to retrieve audit log",
                Status = 500
            });
        }
    }

    /// <summary>
    /// Export prompt configuration to file
    /// </summary>
    /// <param name="version">Specific version to export (optional, defaults to current)</param>
    /// <returns>Path to the exported configuration file</returns>
    [HttpPost("export")]
    [ProducesResponseType(typeof(PromptConfigurationResponse), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    [ProducesResponseType(typeof(ProblemDetails), 500)]
    public async Task<IActionResult> ExportConfiguration([FromQuery] string? version = null)
    {
        try
        {
            var exportPath = await _promptProvider.ExportConfigurationAsync(version);
            
            _logger.LogInformation("Exported prompt configuration {Version} to {ExportPath}", 
                version ?? "current", exportPath);
            
            return Ok(new PromptConfigurationResponse
            {
                Success = true,
                Data = exportPath,
                Metadata = new Dictionary<string, object>
                {
                    ["exportPath"] = exportPath,
                    ["version"] = version ?? "current",
                    ["exportedAt"] = DateTime.UtcNow
                }
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Version not found during export: {Version}", version);
            return NotFound(new ProblemDetails
            {
                Title = "Version Not Found",
                Detail = ex.Message,
                Status = 404
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export prompt configuration version {Version}", version);
            return StatusCode(500, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "Failed to export configuration",
                Status = 500
            });
        }
    }

    /// <summary>
    /// Import prompt configuration from file
    /// </summary>
    /// <param name="importFilePath">Path to the configuration file to import</param>
    /// <param name="request">Import request containing user and merge strategy</param>
    /// <returns>Success status of the import operation</returns>
    [HttpPost("import")]
    [ProducesResponseType(typeof(PromptConfigurationResponse), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    [ProducesResponseType(typeof(ProblemDetails), 500)]
    public async Task<IActionResult> ImportConfiguration([FromQuery] string importFilePath, [FromBody] PromptImportRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(importFilePath))
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid Request",
                    Detail = "Import file path is required",
                    Status = 400
                });
            }

            if (string.IsNullOrWhiteSpace(request.ImportedBy))
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid Request",
                    Detail = "ImportedBy field is required",
                    Status = 400
                });
            }

            if (!System.IO.File.Exists(importFilePath))
            {
                return NotFound(new ProblemDetails
                {
                    Title = "File Not Found",
                    Detail = $"Import file not found: {importFilePath}",
                    Status = 404
                });
            }

            var success = await _promptProvider.ImportConfigurationAsync(importFilePath, request.ImportedBy, request.MergeStrategy);

            if (!success)
            {
                return StatusCode(500, new ProblemDetails
                {
                    Title = "Import Failed",
                    Detail = "Failed to import prompt configuration",
                    Status = 500
                });
            }

            _logger.LogInformation("Imported prompt configuration from {ImportPath} by {User} using {Strategy} strategy", 
                importFilePath, request.ImportedBy, request.MergeStrategy);

            return Ok(new PromptConfigurationResponse
            {
                Success = true,
                Data = importFilePath,
                Metadata = new Dictionary<string, object>
                {
                    ["importPath"] = importFilePath,
                    ["importedBy"] = request.ImportedBy,
                    ["mergeStrategy"] = request.MergeStrategy.ToString(),
                    ["importedAt"] = DateTime.UtcNow
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import prompt configuration from {ImportPath}", importFilePath);
            return StatusCode(500, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "Failed to import configuration",
                Status = 500
            });
        }
    }

    /// <summary>
    /// Get current prompt configuration metadata
    /// </summary>
    /// <returns>Configuration metadata including version info and change history</returns>
    [HttpGet("metadata")]
    [ProducesResponseType(typeof(PromptMetadata), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 500)]
    public async Task<IActionResult> GetConfigurationMetadata()
    {
        try
        {
            var metadata = await _promptProvider.GetConfigurationMetadataAsync();
            
            if (metadata == null)
            {
                return Ok(new PromptMetadata()); // Return empty metadata if none exists
            }
            
            _logger.LogDebug("Retrieved prompt configuration metadata for version {Version}", metadata.Version);
            
            return Ok(metadata);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve prompt configuration metadata");
            return StatusCode(500, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "Failed to retrieve configuration metadata",
                Status = 500
            });
        }
    }
}
