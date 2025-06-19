# EMMA ActionType Scope Classification Worksheet

## Overview

This document provides the authoritative mapping of ActionTypes to their appropriate ActionScope for the three-tier validation framework. Each classification includes detailed rationale for audit traceability and compliance requirements.

## Scope Definitions

- **RealWorld**: External actions with direct real-world impact (full validation, human approval workflows)
- **Hybrid**: Internal actions that significantly impact downstream external actions (moderate validation, conditional approval)
- **InnerWorld**: Agent-to-agent coordination actions (minimal validation, auto-approval)

## ActionType Classification Matrix

| ActionType                 | Scope      | ReasonForScopeAssignment                                                    |
|----------------------------|------------|-----------------------------------------------------------------------------|
| **SendSMS**                | RealWorld  | Triggers an outbound communication to an external party via Twilio          |
| **SendEmail**              | RealWorld  | Initiates external email communication through SMTP/API                     |
| **MakePhoneCall**          | RealWorld  | Places an actual voice call, affecting real-world user experience           |
| **UpdateCRM**              | RealWorld  | Directly modifies customer records in external CRM systems                  |
| **ScheduleMeeting**        | RealWorld  | Creates calendar events with real-world scheduling implications             |
| **SendNotification**       | RealWorld  | Sends external push or in-app notifications to end users                    |
| **ProcessPayment**         | RealWorld  | Triggers billing or subscription events via Stripe                          |
| **AssignResource**         | RealWorld  | Engages external service providers or contractors                           |
| **SyncToCrm**              | RealWorld  | Pushes data changes to external CRM systems, modifies customer records     |
| **ScheduleFollowup**       | RealWorld  | Creates time-based external actions that will execute later                |
| **CreateCalendarEvent**    | RealWorld  | Directly modifies external calendar systems with real-world scheduling     |
| **GenerateFollowUpTask**   | Hybrid     | Creates internal task that will likely result in an external action         |
| **EscalateLead**           | Hybrid     | Triggers higher-priority processing that may generate real-world actions    |
| **GenerateReport**         | Hybrid     | Produces output that might be shared externally or used for decision-making |
| **ValidateCompliance**     | Hybrid     | Internal check that determines if external actions can proceed              |
| **RiskAssessment**         | Hybrid     | Internal scoring that influences downstream external action approval        |
| **OrchestrationDecision**  | Hybrid     | Agent-to-agent coordination that triggers external workflow sequences       |
| **UpdateContactStatus**    | Hybrid     | Internal status change that may trigger external notification workflows    |
| **GenerateRecommendation** | InnerWorld | Provides internal suggestions between agents, no direct external output     |
| **ClassifyIntent**         | InnerWorld | Used for internal routing logic based on interpreted intent                 |
| **EnrichContext**          | InnerWorld | Augments agent memory and context for better decisions                      |
| **SummarizeInteraction**   | InnerWorld | Performs internal context summarization without real-world side effects     |
| **AuditTrailUpdate**       | InnerWorld | Maintains internal logs without changing external state                     |
| **UpdatePromptTemplate**   | InnerWorld | Reconfigures internal LLM prompt templates                                  |
| **RefreshEnumConfig**      | InnerWorld | Updates dropdowns and internal validation lists                             |
| **AnalyzeSentiment**       | InnerWorld | Internal analysis for agent decision-making, no external output            |
| **RetrieveContext**        | InnerWorld | Fetches internal data for agent processing, read-only operation            |
| **GenerateInsight**        | InnerWorld | Creates internal intelligence for agent consumption, no external impact    |

## Validation Intensity by Scope

| Component | InnerWorld | Hybrid | RealWorld |
|-----------|------------|--------|-----------|
| **Relevance Check** | Schema validation | Moderate LLM | Full LLM |
| **Risk Assessment** | Basic field checks | Automated scoring | Comprehensive evaluation |
| **Approval Workflow** | Auto-approve | Conditional (confidence-based) | Full pipeline |
| **Confidence Threshold** | 0.5+ (boost to 0.7) | 0.7+ | 0.8+ |
| **Audit Logging** | Debug level | Structured logging | Comprehensive tracking |
| **Processing Time** | ~90% reduction | ~50% reduction | Current standard |

## Usage Guidelines

### For Developers
1. **Tag Actions**: Use this mapping when creating new ScheduledAction instances
2. **Set Rationale**: Include the ReasonForScopeAssignment from this table for audit compliance
3. **Default Safety**: When in doubt, default to RealWorld scope for maximum validation

### For Product Managers
1. **New ActionTypes**: Add new action types to this document before implementation
2. **Scope Review**: Regularly review classifications as business requirements evolve
3. **Performance Monitoring**: Track validation metrics per scope for optimization opportunities

### For Compliance Teams
1. **Audit Trail**: Use ReasonForScopeAssignment for regulatory compliance documentation
2. **Risk Assessment**: Review Hybrid and RealWorld classifications for risk management
3. **Change Control**: Approve scope changes through formal change management process

## Special Considerations

### Edge Cases
- **GenerateReport**: May need sub-classification based on intended audience (internal vs. external)
- **EscalateLead**: Could be RealWorld if it immediately triggers external notifications
- **Unknown ActionTypes**: Default to RealWorld scope with mandatory human review

### Future Enhancements
Consider adding these columns as the system matures:
- **Confidence Threshold**: Minimum confidence required per scope
- **Approval Required**: Boolean flag for human-in-the-loop requirements  
- **Performance Impact**: Expected processing time category
- **Risk Level**: Low/Medium/High for additional validation context

## Maintenance

### Document Ownership
- **Primary Owner**: Platform Engineering Team
- **Reviewers**: Product Management, Compliance Team
- **Update Frequency**: As needed when new ActionTypes are introduced

### Change Process
1. Propose new ActionType classification via GitHub issue
2. Review with stakeholders (Engineering, Product, Compliance)
3. Update this document with rationale
4. Update validation code and tests
5. Deploy with appropriate monitoring

## References

- [Three-Tier Validation Framework Implementation](../TODO.md#three-tier-validation-framework-implementation)
- [Agent Action Validator](../Emma.Core/Services/AgentActionValidator.cs)
- [ActionScope Enum Definition](../Emma.Core/Models/AgentModels.cs)

---

**Last Updated**: 2025-06-09  
**Version**: 1.0  
**Next Review**: As needed for new ActionTypes
