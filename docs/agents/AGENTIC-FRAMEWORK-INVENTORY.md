# EMMA Agentic Framework Inventory

## 📋 Overview

This document provides a comprehensive inventory of all AI agents within the EMMA platform, categorized by their operational scope and impact. This classification is essential for implementing the three-tier validation framework and understanding agent interaction patterns.

## 🌐 Real-World Agents (External Impact)

*Agents that directly affect external systems, data, or end-users*

| Agent Name | Description | Primary Functions | External Systems | Validation Level |
|------------|-------------|-------------------|------------------|------------------|
| **SMSAgent** | Sends proactive outbound text messages via Twilio integration | • Send SMS notifications<br>• Delivery tracking<br>• Opt-out management | Twilio API | **FULL** |
| **EmailAgent** | Sends transactional or nurturing emails directly through SMTP/API services | • Send emails<br>• Template management<br>• Delivery tracking | SMTP/SendGrid | **FULL** |
| **CallAgent** | Initiates automated voice calls or scheduled callbacks | • Voice call initiation<br>• Callback scheduling<br>• Call logging | VoIP providers | **FULL** |
| **CRMUpdateAgent** | Performs direct updates to external CRM systems (Follow Up Boss) | • Contact synchronization<br>• Property updates<br>• Activity logging | Follow Up Boss API | **FULL** |
| **BillingAgent** | Handles billing processes, payments, and subscription updates via Stripe | • Payment processing<br>• Subscription management<br>• Invoice generation | Stripe API | **FULL** |
| **CalendarAgent** | Schedules appointments, tasks, and updates external calendars | • Appointment scheduling<br>• Calendar synchronization<br>• Reminder management | Google/Outlook Calendar | **FULL** |
| **NotificationAgent** | Sends real-time alerts and notifications to users via push or email | • Push notifications<br>• Alert management<br>• User preferences | Push notification services | **FULL** |
| **ResourceAgent** | Manages assignment or procurement of external resources (e.g., contractors, vendors) | • Resource allocation<br>• Vendor management<br>• Service coordination | External vendor systems | **FULL** |

> Deprecated: Rename to `ServiceProviderAgent` and ensure all assignments operate via the contact-centric model (`Contact` with `RelationshipState.ServiceProvider` + `ContactAssignment`). See `docs/architecture/UNIFIED_SCHEMA.md` and `docs/development/TERMINOLOGY-MIGRATION-GUIDE.md`.

**Total Real-World Agents: 8**

---

## 📦 Inner-World Agents (Internal Processing)

*Agents focused on context enrichment, internal communication, and agent-to-agent interactions*

| Agent Name | Description | Primary Functions | Internal Systems | Validation Level |
|------------|-------------|-------------------|------------------|------------------|
| **ContextAgent** | Enriches and maintains detailed context histories using RAG techniques | • Context enrichment<br>• History management<br>• RAG processing | Internal databases | **MINIMAL** |
| **IntentClassificationAgent** | Identifies user intentions from interactions for accurate routing | • Intent detection<br>• Classification scoring<br>• Routing decisions | Internal ML models | **HYBRID** |
| **RecommendationAgent** | Generates internal recommendations for next best actions (NBA) | • Action recommendations<br>• Priority scoring<br>• Strategy suggestions | Internal algorithms | **HYBRID** |
| **OrchestratorAgent** | Coordinates interactions between multiple agents and maintains workflow state | • Agent coordination<br>• Workflow management<br>• State tracking | Internal orchestration | **HYBRID** |
| **SummaryAgent** | Performs rolling summarization of context and interactions internally | • Content summarization<br>• Key point extraction<br>• History compression | Internal processing | **MINIMAL** |
| **DataAnalysisAgent** | Analyzes interaction data and user behavior to inform decisions | • Behavioral analysis<br>• Pattern recognition<br>• Insight generation | Internal analytics | **MINIMAL** |
| **ComplianceAgent** | Ensures internal processes adhere to predefined rules and policies | • Rule validation<br>• Policy enforcement<br>• Compliance reporting | Internal compliance engine | **HYBRID** |
| **RiskAssessmentAgent** | Evaluates risk of proposed actions, validating before real-world execution | • Risk scoring<br>• Threat assessment<br>• Safety validation | Internal risk models | **HYBRID** |
| **PromptAgent** | Manages and version-controls prompt templates internally | • Template management<br>• Version control<br>• Prompt optimization | Internal template store | **MINIMAL** |
| **EnumManagementAgent** | Maintains versioned enums for internal configurations dynamically | • Enum versioning<br>• Configuration management<br>• Dynamic updates | Internal configuration | **MINIMAL** |

**Total Inner-World Agents: 10**

---

## 🔄 Agent Interaction Patterns

### High-Frequency Coordination Flows

```
ContextAgent ↔ RecommendationAgent ↔ OrchestratorAgent
     ↓                    ↓                    ↓
SummaryAgent ←→ IntentClassificationAgent ←→ RiskAssessmentAgent
     ↓                    ↓                    ↓
DataAnalysisAgent ←→ ComplianceAgent ←→ PromptAgent
```

### Real-World Execution Flows

```
OrchestratorAgent → RiskAssessmentAgent → ComplianceAgent
                                              ↓
SMSAgent / EmailAgent / CallAgent / CRMUpdateAgent / etc.
```

---

## 📊 Validation Framework Mapping

### Three-Tier Validation Levels

| Validation Level | Agent Count | Characteristics | Performance Target |
|------------------|-------------|-----------------|-------------------|
| **MINIMAL** | 5 agents | High-frequency, low-risk internal operations | <10ms validation |
| **HYBRID** | 5 agents | Critical internal decisions affecting external actions | <100ms validation |
| **FULL** | 8 agents | Direct external impact requiring comprehensive validation | <1000ms validation |

### Validation Intensity Matrix

| Component | MINIMAL | HYBRID | FULL |
|-----------|---------|--------|------|
| **Relevance Check** | Schema validation only | Basic LLM assessment | Comprehensive LLM evaluation |
| **Risk Assessment** | Pre-computed (Low) | Automated risk scoring | Full risk analysis |
| **Approval Workflow** | Auto-approve | Conditional approval | Full approval pipeline |
| **Confidence Scoring** | Basic threshold (0.5+) | Moderate threshold (0.7+) | Strict threshold (0.8+) |
| **Audit Logging** | Lightweight structured | Standard structured | Comprehensive detailed |
| **LLM Calls** | None for validation | Minimal/cached | Full validation calls |

---

## 🚀 Hot-Launch Agent Framework

### Agent Lifecycle Management

```
Design → Generate → Test → Deploy → Monitor → Update
   ↓        ↓        ↓       ↓        ↓        ↓
Product  AI Code   Unit   Production Live     Version
Manager  Generator Tests   Deploy   Metrics   Control
```

### Agent Generation Requirements

1. **Agent Specification**
   - Name and description
   - Scope classification (Real-World/Inner-World/Hybrid)
   - Primary functions and capabilities
   - External system integrations
   - Validation requirements

2. **Code Generation**
   - Interface implementation
   - Service registration
   - Dependency injection setup
   - Basic error handling
   - Logging integration

3. **Testing Framework**
   - Unit test generation
   - Integration test templates
   - Mock service setup
   - Performance benchmarks

4. **Deployment Pipeline**
   - Configuration management
   - Feature flag integration
   - Rollback capabilities
   - Monitoring setup

---

## 📈 Performance Considerations

### Expected Agent Activity Levels

| Agent Type | Actions/Hour | Peak Load | Validation Overhead |
|------------|--------------|-----------|-------------------|
| **Inner-World** | 1000-10000 | High-frequency bursts | Must be minimal |
| **Hybrid** | 100-1000 | Moderate sustained | Optimized for speed |
| **Real-World** | 10-100 | Low but critical | Full validation acceptable |

### Optimization Strategies

1. **Caching**: Pre-computed validation results for common patterns
2. **Batching**: Group similar inner-world actions for bulk processing
3. **Circuit Breakers**: Fast-fail for overloaded validation services
4. **Async Processing**: Non-blocking validation for non-critical paths

---

## 🔮 Future Agent Roadmap

### Planned Agents (Next Quarter)

| Agent Name | Type | Priority | Description |
|------------|------|----------|-------------|
| **LeadIntakeAgent** | Real-World | High | Automated lead capture and qualification |
| **PropertyInterestAgent** | Hybrid | High | Property matching and interest tracking |
| **ReEngagementAgent** | Real-World | Medium | Automated re-engagement campaigns |
| **MarketAnalysisAgent** | Inner-World | Medium | Market data analysis and insights |
| **PredictiveAgent** | Inner-World | Low | Predictive analytics for lead scoring |

### Scaling Considerations

- **Agent Density**: Current 18 agents → Target 50+ agents
- **Interaction Complexity**: Linear → Mesh network coordination
- **Performance Requirements**: Sub-second response times
- **Validation Efficiency**: 90%+ inner-world actions with minimal validation

---

## 🎯 Strategic Impact

This agentic framework inventory enables:

1. **Performance Optimization**: Targeted validation intensity based on agent scope
2. **Scalable Architecture**: Clear patterns for adding new agents
3. **Operational Excellence**: Comprehensive monitoring and management
4. **Product Agility**: Rapid deployment of new AI capabilities
5. **Risk Management**: Appropriate validation for external impact

The three-tier validation framework ensures optimal performance while maintaining security and compliance across all agent operations.
