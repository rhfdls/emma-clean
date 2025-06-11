# EMMA Configuration Management Guide

## Overview

The EMMA AI Agent platform includes two enterprise-grade configuration management facilities that provide versioning, audit trails, rollback capabilities, and hot-reload functionality for critical system configurations:

1. **Enum Management System** - Dynamic enum configurations for dropdowns, validation rules, and business logic
2. **Prompt Management System** - AI prompt templates, agent instructions, and industry-specific configurations

Both systems implement identical governance patterns with comprehensive audit logging, version control, and operational safety features.

---

## 🔧 Enum Management System

### Purpose
Manages dynamic enum configurations that drive business logic, validation rules, and UI dropdowns without requiring code deployments.

### Key Features
- **Dynamic Enum Loading**: Hot-reload enum configurations without application restart
- **Version Control**: Full versioning with rollback capabilities
- **Audit Trail**: Complete change history with user attribution
- **Import/Export**: Configuration portability and backup
- **Integrity Verification**: SHA256 hash validation for tamper detection
- **Multi-Environment Support**: Environment-specific configurations

### Architecture

```
EnumProvider (Core Service)
├── IEnumProvider (Interface)
├── EnumVersioningService (Versioning Logic)
├── EnumModels.cs (Data Models)
└── EnumExtensions.cs (Helper Methods)
```

### Configuration Structure

```json
{
  "metadata": {
    "version": "v1.2.3",
    "lastModified": "2025-06-09T15:30:00Z",
    "modifiedBy": "admin@company.com",
    "versionHistory": [...]
  },
  "enums": {
    "ContactType": {
      "values": [
        { "key": "Lead", "value": "Lead", "displayName": "Lead" },
        { "key": "Client", "value": "Client", "displayName": "Active Client" },
        { "key": "Prospect", "value": "Prospect", "displayName": "Prospect" }
      ],
      "metadata": {
        "description": "Types of contacts in the system",
        "category": "Contact Management"
      }
    }
  }
}
```

### API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/enums` | Get all enum configurations |
| GET | `/api/enums/{enumName}` | Get specific enum |
| POST | `/api/enums/versions` | Create new version |
| POST | `/api/enums/rollback` | Rollback to version |
| GET | `/api/enums/history` | Version history |
| GET | `/api/enums/audit` | Audit log |
| POST | `/api/enums/export` | Export configuration |
| POST | `/api/enums/import` | Import configuration |

---

## 🤖 Prompt Management System

### Purpose
Manages AI prompt templates, system instructions, and industry-specific configurations for the AI agent ecosystem.

### Key Features
- **Agent-Specific Prompts**: Tailored instructions for each AI agent type
- **Industry Profiles**: Industry-specific prompt overrides and templates
- **Template Inheritance**: Global templates with agent-specific overrides
- **Context Templates**: Dynamic prompt building with variable substitution
- **Action Templates**: Specific prompts for different agent actions
- **Version Control**: Full versioning with rollback capabilities
- **Hot Reload**: Dynamic prompt updates without service restart

### Architecture

```
PromptProvider (Core Service)
├── IPromptProvider (Interface)
├── PromptVersioningService (Versioning Logic)
├── PromptModels.cs (Data Models)
├── PromptTemplates.cs (Template Engine)
└── PromptVersioningController (REST API)
```

### Configuration Structure

```json
{
  "metadata": {
    "version": "v2.1.0",
    "lastModified": "2025-06-09T15:30:00Z",
    "modifiedBy": "ai-admin@company.com"
  },
  "agents": {
    "NbaAgent": {
      "systemPrompt": "You are an expert NBA (Next Best Action) agent...",
      "contextTemplates": {
        "contactContext": "Contact: {{contactName}}\nStage: {{currentStage}}...",
        "interactionHistory": "Recent interactions:\n{{#each interactions}}..."
      },
      "actionTemplates": {
        "sendEmail": "Generate a personalized email for {{contactName}}...",
        "scheduleFollowup": "Determine optimal follow-up timing..."
      },
      "responseFormats": {
        "recommendation": {
          "action": "string",
          "confidence": "number",
          "reasoning": "string",
          "payload": "object"
        }
      }
    }
  },
  "industries": {
    "RealEstate": {
      "systemPromptOverride": "You are a real estate industry expert...",
      "contextEnhancements": {
        "marketData": "Current market conditions: {{marketTrends}}...",
        "propertyDetails": "Property: {{address}}, Price: {{price}}..."
      }
    }
  },
  "globalTemplates": {
    "companyInfo": "Company: {{companyName}}\nIndustry: {{industry}}...",
    "userContext": "User: {{userName}}\nRole: {{userRole}}..."
  }
}
```

### API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/prompts` | Get all prompt configurations |
| GET | `/api/prompts/{agentName}` | Get agent-specific prompts |
| POST | `/api/promptversioning/versions` | Create new version |
| POST | `/api/promptversioning/rollback` | Rollback to version |
| GET | `/api/promptversioning/history` | Version history |
| GET | `/api/promptversioning/compare` | Compare versions |
| GET | `/api/promptversioning/audit` | Audit log |
| POST | `/api/promptversioning/export` | Export configuration |
| POST | `/api/promptversioning/import` | Import configuration |

---

## 🔒 Security & Governance

### Audit Logging
Both systems maintain comprehensive audit trails:

```json
{
  "id": "audit-123",
  "timestamp": "2025-06-09T15:30:00Z",
  "changeType": "Update",
  "changedBy": "admin@company.com",
  "description": "Updated ContactType enum values",
  "metadata": {
    "version": "v1.2.3",
    "affectedItems": ["ContactType"],
    "changeReason": "Added new prospect categories"
  }
}
```

### Version Control
- **Automatic Versioning**: Every change creates a new version
- **Semantic Versioning**: Major.Minor.Patch format
- **Rollback Safety**: Can rollback to any previous version
- **Change Validation**: Integrity checks before applying changes

### Access Control
- **Role-Based Access**: Different permissions for viewers, editors, admins
- **User Attribution**: All changes tracked to specific users
- **Approval Workflows**: Optional approval process for critical changes

---

## 🎨 UI/UX Design Recommendations

### 1. Configuration Dashboard

**Layout**: Split-screen design with navigation sidebar

```
┌─────────────────────────────────────────────────────────┐
│ [EMMA Logo]                                    [User] ▼ │
├─────────────────────────────────────────────────────────┤
│ ├─ 📊 Dashboard     │                                   │
│ ├─ 🔧 Enums         │     Configuration Overview        │
│ │  ├─ ContactType   │                                   │
│ │  ├─ LeadStatus    │   📈 Recent Changes: 5            │
│ │  └─ Priority      │   🔄 Active Version: v2.1.0       │
│ ├─ 🤖 Prompts       │   👥 Contributors: 3              │
│ │  ├─ NBA Agent     │   ⚠️  Pending Reviews: 2          │
│ │  ├─ Context Agent │                                   │
│ │  └─ Industries    │                                   │
│ ├─ 📋 Audit Logs    │                                   │
│ ├─ 🔄 Versions      │                                   │
│ └─ ⚙️  Settings     │                                   │
└─────────────────────────────────────────────────────────┘
```

### 2. Enum Configuration Editor

**Features**:
- **Live Preview**: Real-time preview of enum dropdowns
- **Drag & Drop**: Reorder enum values
- **Bulk Operations**: Import/export via CSV or JSON
- **Validation**: Real-time validation with error highlighting

```
┌─────────────────────────────────────────────────────────┐
│ ContactType Enum                           [Save] [Preview] │
├─────────────────────────────────────────────────────────┤
│ Description: Types of contacts in CRM system            │
│ Category: Contact Management                            │
│                                                         │
│ Values:                                    [+ Add Value] │
│ ┌─────────────────────────────────────────────────────┐ │
│ │ 🔸 Lead          │ Lead          │ [Edit] [Delete] │ │
│ │ 🔸 Prospect      │ Prospect      │ [Edit] [Delete] │ │
│ │ 🔸 Client        │ Active Client │ [Edit] [Delete] │ │
│ │ 🔸 Inactive      │ Inactive      │ [Edit] [Delete] │ │
│ └─────────────────────────────────────────────────────┘ │
│                                                         │
│ Preview:                                                │
│ [Dropdown: Lead ▼]                                      │
└─────────────────────────────────────────────────────────┘
```

### 3. Prompt Template Editor

**Features**:
- **Syntax Highlighting**: Markdown/template syntax highlighting
- **Variable Autocomplete**: IntelliSense for template variables
- **Live Testing**: Test prompts with sample data
- **Version Comparison**: Side-by-side diff view

```
┌─────────────────────────────────────────────────────────┐
│ NBA Agent - System Prompt                    [Test] [Save] │
├─────────────────────────────────────────────────────────┤
│ Agent: NbaAgent                    Industry: Real Estate │
│                                                         │
│ ┌─ System Prompt ─────────────────────────────────────┐ │
│ │ You are an expert NBA (Next Best Action) agent     │ │
│ │ specializing in {{industry}} industry.             │ │
│ │                                                     │ │
│ │ Your role is to analyze contact interactions and    │ │
│ │ recommend the most appropriate next action based    │ │
│ │ on:                                                 │ │
│ │ - Contact stage: {{currentStage}}                   │ │
│ │ - Recent interactions: {{recentInteractions}}      │ │
│ │ - Engagement metrics: {{engagementMetrics}}        │ │
│ └─────────────────────────────────────────────────────┘ │
│                                                         │
│ Variables Available:                                    │
│ • {{contactName}} • {{currentStage}} • {{industry}}    │
│ • {{recentInteractions}} • {{engagementMetrics}}       │
│                                                         │
│ [Test with Sample Data] [Preview Output] [Save Draft]   │
└─────────────────────────────────────────────────────────┘
```

### 4. Version History & Rollback

**Features**:
- **Timeline View**: Visual timeline of changes
- **Diff Viewer**: Detailed change comparison
- **One-Click Rollback**: Safe rollback with confirmation
- **Branch Visualization**: Show version relationships

```
┌─────────────────────────────────────────────────────────┐
│ Version History - ContactType Enum                      │
├─────────────────────────────────────────────────────────┤
│                                                         │
│ ● v1.2.3 (Current) - 2025-06-09 15:30                 │
│ │ 👤 admin@company.com                                  │
│ │ 📝 Added "Inactive" status for archived contacts     │
│ │ [View] [Compare] [Rollback]                          │
│ │                                                       │
│ ● v1.2.2 - 2025-06-08 14:15                           │
│ │ 👤 user@company.com                                   │
│ │ 📝 Updated display names for better clarity          │
│ │ [View] [Compare] [Rollback]                          │
│ │                                                       │
│ ● v1.2.1 - 2025-06-07 09:45                           │
│ │ 👤 admin@company.com                                  │
│ │ 📝 Initial ContactType enum configuration            │
│ │ [View] [Compare] [Rollback]                          │
│                                                         │
│ [Export History] [Compare Selected] [Bulk Rollback]     │
└─────────────────────────────────────────────────────────┘
```

### 5. Audit Log Viewer

**Features**:
- **Advanced Filtering**: Filter by user, date, change type
- **Search**: Full-text search across all changes
- **Export**: Export audit logs for compliance
- **Real-time Updates**: Live updates as changes occur

```
┌─────────────────────────────────────────────────────────┐
│ Audit Log                                    [Export CSV] │
├─────────────────────────────────────────────────────────┤
│ Filters: [All Users ▼] [Last 30 Days ▼] [All Types ▼]   │
│ Search: [                                    ] [🔍]     │
│                                                         │
│ ┌─────────────────────────────────────────────────────┐ │
│ │ 🔄 UPDATE │ 2025-06-09 15:30 │ admin@company.com   │ │
│ │           │ ContactType Enum │ Added Inactive status│ │
│ │           │ [View Details] [View Changes]          │ │
│ ├─────────────────────────────────────────────────────┤ │
│ │ ✅ CREATE │ 2025-06-08 14:15 │ user@company.com    │ │
│ │           │ NBA Agent Prompt │ Created new template │ │
│ │           │ [View Details] [View Changes]          │ │
│ ├─────────────────────────────────────────────────────┤ │
│ │ ⚠️  ROLLBACK│ 2025-06-07 09:45 │ admin@company.com │ │
│ │           │ LeadStatus Enum  │ Rolled back to v1.1 │ │
│ │           │ [View Details] [View Changes]          │ │
│ └─────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────┘
```

### 6. Import/Export Interface

**Features**:
- **Drag & Drop**: Drag files to upload
- **Format Validation**: Validate JSON/CSV before import
- **Preview Changes**: Show what will change before applying
- **Merge Strategies**: Choose how to handle conflicts

```
┌─────────────────────────────────────────────────────────┐
│ Import Configuration                                    │
├─────────────────────────────────────────────────────────┤
│                                                         │
│ ┌─────────────────────────────────────────────────────┐ │
│ │                                                     │ │
│ │     📁 Drag & Drop Files Here                       │ │
│ │        or [Browse Files]                            │ │
│ │                                                     │ │
│ │     Supported: .json, .csv, .xlsx                   │ │
│ │                                                     │ │
│ └─────────────────────────────────────────────────────┘ │
│                                                         │
│ Merge Strategy:                                         │
│ ○ Replace All    ● Merge (Keep Existing)               │
│ ○ Merge (Override) ○ Preview Only                      │
│                                                         │
│ Options:                                                │
│ ☑ Create backup before import                          │
│ ☑ Validate configuration integrity                     │
│ ☐ Skip items with validation errors                    │
│                                                         │
│ [Cancel] [Preview Changes] [Import]                     │
└─────────────────────────────────────────────────────────┘
```

---

## 🚀 Implementation Recommendations

### Phase 1: Core Dashboard
1. **Configuration Overview Dashboard**
2. **Basic Enum Editor**
3. **Simple Prompt Editor**
4. **Version History Viewer**

### Phase 2: Advanced Features
1. **Advanced Diff Viewer**
2. **Bulk Operations**
3. **Import/Export UI**
4. **Advanced Filtering**

### Phase 3: Enterprise Features
1. **Approval Workflows**
2. **Role-Based Access Control**
3. **Advanced Analytics**
4. **Integration APIs**

### Technology Stack Recommendations

**Frontend**:
- **React** with TypeScript for type safety
- **Material-UI** or **Ant Design** for consistent components
- **Monaco Editor** for code/template editing
- **React-DnD** for drag & drop functionality

**State Management**:
- **Redux Toolkit** for complex state management
- **React Query** for API state management

**Visualization**:
- **D3.js** or **Recharts** for version timeline visualization
- **React-Diff-Viewer** for change comparison

---

## 📋 Best Practices

### Configuration Management
1. **Always create backups** before major changes
2. **Use descriptive version descriptions** for easy identification
3. **Test configurations** in staging before production
4. **Regular audits** of configuration changes
5. **Document business impact** of configuration changes

### Security
1. **Implement proper authentication** and authorization
2. **Audit all configuration access** and changes
3. **Use HTTPS** for all API communications
4. **Validate all inputs** to prevent injection attacks
5. **Regular security reviews** of configuration data

### Performance
1. **Cache frequently accessed** configurations
2. **Implement pagination** for large configuration lists
3. **Use compression** for configuration exports
4. **Monitor API performance** and optimize as needed
5. **Implement rate limiting** to prevent abuse

---

This comprehensive configuration management system provides enterprise-grade governance, operational safety, and user-friendly interfaces for managing both enum and prompt configurations in the EMMA AI Agent platform.
