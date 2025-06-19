# Agent Capabilities Configuration Guide

This document describes how to configure agent capabilities using YAML files in the EMMA platform.

## Table of Contents
- [Overview](#overview)
- [File Format](#file-format)
- [Schema Reference](#schema-reference)
- [Examples](#examples)
- [Validation Rules](#validation-rules)
- [Hot Reloading](#hot-reloading)
- [Best Practices](#best-practices)

## Overview

Agent capabilities define what actions and features are available to different AI agents in the EMMA platform. These capabilities are configured using YAML files that can be modified without requiring code changes.

## File Format

Capability configurations are stored in YAML files with the following structure:

```yaml
version: "1.0"  # Schema version

agents:
  AgentName:
    capabilities:
      - name: "capability:name"
        description: "Description of what this capability does"
        enabled: true
        validation_rules: {}
    rate_limits:
      - window: "1m"
        max_requests: 100
        scope: "per_tenant"
```

## Schema Reference

### Root Level

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| version | string | Yes | Schema version (e.g., "1.0") |
| agents | object | Yes | Map of agent configurations |

### Agent Configuration

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| capabilities | array | Yes | List of capabilities for this agent |
| rate_limits | array | No | Rate limiting rules for this agent |

### Capability

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| name | string | Yes | Unique identifier for the capability (lowercase, numbers, colons, hyphens) |
| description | string | Yes | Human-readable description of the capability |
| enabled | boolean | No | Whether the capability is enabled (default: true) |
| validation_rules | object | No | Custom validation rules for this capability |

### Rate Limit

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| window | string | Yes | Time window (e.g., "1s", "5m", "2h") |
| max_requests | integer | Yes | Maximum number of requests allowed in the window |
| scope | string | Yes | Scope of the rate limit ("per_tenant" or "global") |

## Examples

### Basic Configuration

```yaml
version: "1.0"

agents:
  NbaAgent:
    capabilities:
      - name: "suggest:action"
        description: "Suggest next best actions for a conversation"
        enabled: true
      - name: "analyze:sentiment"
        description: "Analyze sentiment of conversation"
        enabled: true
```

### With Rate Limiting

```yaml
version: "1.0"

agents:
  ContextIntelligenceAgent:
    capabilities:
      - name: "extract:entities"
        description: "Extract entities from text"
        enabled: true
      - name: "classify:intent"
        description: "Classify user intent"
        enabled: true
    rate_limits:
      - window: "1s"
        max_requests: 10
        scope: "per_tenant"
      - window: "1m"
        max_requests: 600
        scope: "global"
```

### With Validation Rules

```yaml
version: "1.0"

agents:
  ResourceAgent:
    capabilities:
      - name: "suggest:resource"
        description: "Suggest relevant resources"
        enabled: true
        validation_rules:
          max_suggestions: 5
          allowed_categories: ["article", "video"]
          min_confidence: 0.7
```

## Validation Rules

### Version
- Must be in format "major.minor" (e.g., "1.0")
- Required field

### Agent Names
- Must not be empty
- Should use PascalCase (e.g., "NbaAgent", "ContextIntelligenceAgent")

### Capability Names
- Must be lowercase
- Can contain letters, numbers, colons, and hyphens
- Should follow the pattern "verb:object" (e.g., "suggest:action", "extract:entities")

### Rate Limits
- Window must end with 's' (seconds), 'm' (minutes), or 'h' (hours)
- max_requests must be greater than 0
- Scope must be either "per_tenant" or "global"

## Hot Reloading

Capability configurations can be reloaded at runtime without restarting the application:

1. Save changes to the YAML file
2. The system will automatically detect changes
3. The new configuration will be validated
4. If valid, the new capabilities will be applied
5. If invalid, an error will be logged and the previous configuration will remain active

## Best Practices

1. **Use Descriptive Names**: Choose clear, descriptive names for capabilities
2. **Document Everything**: Always include descriptions for agents and capabilities
3. **Test Changes**: Test configuration changes in a non-production environment first
4. **Use Version Control**: Track changes to configuration files in version control
5. **Monitor Logs**: Check application logs after making changes to catch any validation errors
6. **Start Simple**: Begin with a minimal configuration and add complexity as needed
7. **Use Comments**: Add comments to explain non-obvious configuration choices

## Troubleshooting

### Common Issues

1. **Configuration not loading**
   - Check file permissions
   - Verify YAML syntax (use a YAML validator)
   - Check application logs for errors

2. **Changes not taking effect**
   - Ensure the file is being saved
   - Check for YAML syntax errors
   - Verify the file path in application configuration

3. **Validation errors**
   - Check the exact error message in the logs
   - Verify all required fields are present
   - Check field formats (e.g., version must be "major.minor")
