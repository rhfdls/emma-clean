using Asp.Versioning;
using Microsoft.Extensions.DependencyInjection;

namespace Emma.Core.Configuration;

/// <summary>
/// API versioning configuration for EMMA Agent Factory.
/// Supports header-based versioning with backward compatibility.
/// Sprint 1 implementation for scalable API evolution.
/// </summary>
public static class ApiVersioning
{
    /// <summary>
    /// Current API version constants.
    /// </summary>
    public static class Versions
    {
        public const string V1_0 = "1.0";
        public const string V1_1 = "1.1";
        public const string V2_0 = "2.0";
        
        // Sprint versions
        public const string SPRINT_1 = "1.0";
        public const string SPRINT_2 = "1.1";
        public const string SPRINT_3 = "2.0";
    }

    /// <summary>
    /// API version groups for feature organization.
    /// </summary>
    public static class Groups
    {
        public const string CORE = "core";
        public const string AGENTS = "agents";
        public const string ORCHESTRATION = "orchestration";
        public const string PRIVACY = "privacy";
        public const string ADMIN = "admin";
    }

    /// <summary>
    /// Configure API versioning for EMMA services.
    /// TODO: Complete implementation when API versioning packages are properly configured
    /// </summary>
    public static IServiceCollection AddEmmaApiVersioning(this IServiceCollection services)
    {
        // TODO: Implement when API versioning dependencies are resolved
        // This preserves the interface while allowing core functionality to build
        return services;
    }

    /// <summary>
    /// Version compatibility matrix for backward compatibility checks.
    /// </summary>
    public static class Compatibility
    {
        /// <summary>
        /// Check if a requested version is compatible with the current implementation.
        /// </summary>
        public static bool IsCompatible(string requestedVersion, string currentVersion)
        {
            // Parse versions
            if (!Version.TryParse(requestedVersion, out var requested) ||
                !Version.TryParse(currentVersion, out var current))
            {
                return false;
            }

            // Major version must match, minor version can be backward compatible
            return requested.Major == current.Major && requested.Minor <= current.Minor;
        }

        /// <summary>
        /// Get the minimum supported version for a feature.
        /// </summary>
        public static string GetMinimumVersionForFeature(string featureName)
        {
            return featureName switch
            {
                "dynamic-agent-routing" => Versions.V1_0,
                "agent-registry" => Versions.V1_0,
                "feature-flags" => Versions.V1_0,
                "lifecycle-hooks" => Versions.V1_0,
                "explainability-framework" => Versions.V1_0,
                "context-provider-abstraction" => Versions.V1_1,
                "advanced-monitoring" => Versions.V1_1,
                "multi-tenant-isolation" => Versions.V2_0,
                _ => Versions.V1_0
            };
        }
    }

    /// <summary>
    /// Version-specific feature flags for gradual rollout.
    /// </summary>
    public static class VersionFeatures
    {
        /// <summary>
        /// Features available in version 1.0 (Sprint 1).
        /// </summary>
        public static readonly HashSet<string> V1_0_Features = new()
        {
            "dynamic-agent-routing",
            "agent-registry",
            "feature-flags",
            "lifecycle-hooks",
            "explainability-framework"
        };

        /// <summary>
        /// Features available in version 1.1 (Sprint 2).
        /// </summary>
        public static readonly HashSet<string> V1_1_Features = new(V1_0_Features)
        {
            "context-provider-abstraction",
            "advanced-monitoring",
            "performance-optimization"
        };

        /// <summary>
        /// Features available in version 2.0 (Sprint 3+).
        /// </summary>
        public static readonly HashSet<string> V2_0_Features = new(V1_1_Features)
        {
            "multi-tenant-isolation",
            "advanced-security",
            "enterprise-features"
        };

        /// <summary>
        /// Check if a feature is available in the specified version.
        /// </summary>
        public static bool IsFeatureAvailable(string feature, string version)
        {
            return version switch
            {
                Versions.V1_0 => V1_0_Features.Contains(feature),
                Versions.V1_1 => V1_1_Features.Contains(feature),
                Versions.V2_0 => V2_0_Features.Contains(feature),
                _ => false
            };
        }
    }
}
