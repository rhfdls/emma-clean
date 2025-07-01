using Emma.Core.Interfaces;
using Emma.Core.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Emma.Core.Extensions;

/// <summary>
/// Extension methods for working with dynamic enums
/// Provides convenient helpers for UI binding and validation
/// </summary>
public static class EnumExtensions
{
    /// <summary>
    /// Convert enum values to SelectListItems for MVC dropdowns
    /// </summary>
    public static async Task<IEnumerable<SelectListItem>> ToSelectListAsync(
        this IEnumProvider enumProvider, 
        string enumType, 
        EnumContext? context = null, 
        string? selectedValue = null)
    {
        var values = await enumProvider.GetEnumValuesAsync(enumType, context);
        return values.Select(v => new SelectListItem
        {
            Value = v.Key,
            Text = v.DisplayName,
            Selected = v.Key.Equals(selectedValue, StringComparison.OrdinalIgnoreCase)
        });
    }

    /// <summary>
    /// Convert enum values to SelectListItems with descriptions
    /// </summary>
    public static async Task<IEnumerable<SelectListItem>> ToSelectListWithDescriptionsAsync(
        this IEnumProvider enumProvider, 
        string enumType, 
        EnumContext? context = null, 
        string? selectedValue = null)
    {
        var values = await enumProvider.GetEnumValuesAsync(enumType, context);
        return values.Select(v => new SelectListItem
        {
            Value = v.Key,
            Text = string.IsNullOrEmpty(v.Description) ? v.DisplayName : $"{v.DisplayName} - {v.Description}",
            Selected = v.Key.Equals(selectedValue, StringComparison.OrdinalIgnoreCase)
        });
    }

    /// <summary>
    /// Get enum values formatted for JSON APIs
    /// </summary>
    public static async Task<object> ToApiFormatAsync(
        this IEnumProvider enumProvider, 
        string enumType, 
        EnumContext? context = null)
    {
        var values = await enumProvider.GetEnumValuesAsync(enumType, context);
        var metadata = await enumProvider.GetEnumMetadataAsync(enumType, context);
        
        return new
        {
            enumType = enumType,
            metadata = new
            {
                displayName = metadata?.EnumType ?? enumType,
                valueCount = metadata?.ValueCount ?? 0,
                allowCustomValues = metadata?.AllowCustomValues ?? false,
                defaultValue = metadata?.DefaultValue
            },
            values = values.Select(v => new
            {
                key = v.Key,
                displayName = v.DisplayName,
                description = v.Description,
                order = v.Order,
                isActive = v.IsActive,
                color = v.Color,
                icon = v.Icon,
                metadata = v.Metadata
            })
        };
    }

    /// <summary>
    /// Create enum context from common parameters
    /// </summary>
    public static EnumContext CreateContext(string? industryCode = null, string? agentType = null, string? tenantId = null)
    {
        return new EnumContext
        {
            IndustryCode = industryCode,
            AgentType = agentType,
            TenantId = tenantId
        };
    }

    /// <summary>
    /// Validate multiple enum values at once
    /// </summary>
    public static async Task<Dictionary<string, bool>> ValidateMultipleAsync(
        this IEnumProvider enumProvider,
        string enumType,
        IEnumerable<string> keys,
        EnumContext? context = null)
    {
        var results = new Dictionary<string, bool>();
        
        foreach (var key in keys)
        {
            results[key] = await enumProvider.ValidateEnumValueAsync(enumType, key, context);
        }
        
        return results;
    }

    /// <summary>
    /// Get enum values grouped by metadata category
    /// </summary>
    public static async Task<Dictionary<string, IEnumerable<EnumValue>>> GetGroupedByCategoryAsync(
        this IEnumProvider enumProvider,
        string enumType,
        EnumContext? context = null,
        string categoryMetadataKey = "category")
    {
        var values = await enumProvider.GetEnumValuesAsync(enumType, context);
        
        return values
            .GroupBy(v => v.Metadata.TryGetValue(categoryMetadataKey, out var category) 
                ? category?.ToString() ?? "Other" 
                : "Other")
            .ToDictionary(g => g.Key, g => g.AsEnumerable());
    }

    /// <summary>
    /// Get high priority enum values (based on metadata)
    /// </summary>
    public static async Task<IEnumerable<EnumValue>> GetHighPriorityAsync(
        this IEnumProvider enumProvider,
        string enumType,
        EnumContext? context = null,
        string priorityMetadataKey = "priority")
    {
        var values = await enumProvider.GetEnumValuesAsync(enumType, context);
        
        return values.Where(v => 
            v.Metadata.TryGetValue(priorityMetadataKey, out var priority) && 
            priority?.ToString()?.Equals("high", StringComparison.OrdinalIgnoreCase) == true);
    }

    /// <summary>
    /// Search enum values by display name or description
    /// </summary>
    public static async Task<IEnumerable<EnumValue>> SearchAsync(
        this IEnumProvider enumProvider,
        string enumType,
        string searchTerm,
        EnumContext? context = null)
    {
        var values = await enumProvider.GetEnumValuesAsync(enumType, context);
        
        if (string.IsNullOrWhiteSpace(searchTerm))
            return values;
            
        var lowerSearchTerm = searchTerm.ToLowerInvariant();
        
        return values.Where(v => 
            v.DisplayName.ToLowerInvariant().Contains(lowerSearchTerm) ||
            (v.Description?.ToLowerInvariant().Contains(lowerSearchTerm) == true));
    }
}
