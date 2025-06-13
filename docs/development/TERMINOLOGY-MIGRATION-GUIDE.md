# EMMA Terminology Migration Guide

This document outlines the migration from legacy "conversation" terminology to the standardized "interaction" terminology across the EMMA platform.

---

## Migration Overview

**Objective**: Standardize on "interaction" terminology to align with data models and contact-centric architecture.

**Scope**: All code, APIs, documentation, and configuration using "conversation" terminology.

**Timeline**: Incremental migration during build error resolution phase.

---

## Key Changes

### 1. Core Model Alignment

#### Before (Inconsistent):
```csharp
// Services used "conversation"
public class ConversationContext { }
public Task<ConversationContext> GetConversationContextAsync(Guid conversationId);

// But models used "interaction"  
public class RecentInteraction { }
public Guid InteractionId { get; set; }
```

#### After (Consistent):
```csharp
// All use "interaction"
public class InteractionContext { }
public Task<InteractionContext> GetInteractionContextAsync(Guid interactionId);

public class RecentInteraction { }
public Guid InteractionId { get; set; }
```

### 2. Intelligence Data Structures

#### Before (Rigid Types):
```csharp
public ConversationInsights Insights { get; set; }
public SentimentAnalysis Sentiment { get; set; }
public List<BuyingSignal> BuyingSignals { get; set; }
```

#### After (Flexible Dictionaries):
```csharp
public Dictionary<string, object> Insights { get; set; }
public Dictionary<string, object> Sentiment { get; set; }
public List<string> BuyingSignals { get; set; }
```

---

## Specific File Changes

### ContextProvider.cs
**Status**: 
**Changes Made**:
1. Fixed `Insights` property from `ConversationInsights` to `Dictionary<string, object>`
2. Fixed `Sentiment` property from `SentimentAnalysis` to `Dictionary<string, object>`
3. Fixed `BuyingSignals` from `List<BuyingSignal>` to `List<string>`
4. Fixed `Recommendations` from `List<Recommendation>` to `List<string>`

**Impact**: Resolves build errors and aligns with `ContextIntelligence` interface.

### Pending Changes

#### IContextProvider.cs
**Required Changes**:
- [ ] `ConversationContext` → `InteractionContext`
- [ ] `GetConversationContextAsync` → `GetInteractionContextAsync`
- [ ] `UpdateConversationContextAsync` → `UpdateInteractionContextAsync`
- [ ] Parameter `conversationId` → `interactionId`

#### ContextProvider.cs
**Required Changes**:
- [ ] `_conversationCache` → `_interactionCache`
- [ ] All method names and parameters using "conversation"
- [ ] Internal variable names and comments

#### Other Services
**Files to Review**:
- Any service referencing `ConversationContext`
- API controllers using conversation terminology
- Configuration files with conversation-related keys

---

## Data Model Validation

### Current State 
The data models are already correctly using "interaction" terminology:

```csharp
// Correct - Already using interaction terminology
public class RecentInteraction
{
    public Guid InteractionId { get; set; }
    public Guid ContactId { get; set; }
    public string Type { get; set; } // Email, Call, SMS, Meeting
    // ...
}

// Correct - Data contract specifies interaction
// DATA_CONTRACT.md uses "interaction" throughout

### Service Alignment Required 
Services need to be updated to match the data model terminology:

```csharp
// Current - Services use conversation
public class InteractionContext { }

// Corrected terminology
public class InteractionContext { }

### Internal APIs
**Risk**: MEDIUM - Service-to-service calls may use interaction terminology
**Action**: Update method signatures and parameter names incrementally

### Database Queries
**Risk**: LOW - Database schema already uses interaction terminology
**Action**: No database changes required

---

## Testing Strategy

### Unit Tests
- [ ] Update test method names using interaction terminology
- [ ] Update test data and assertions
- [ ] Verify mock objects use correct terminology

### Integration Tests
- [ ] Test context flow with new terminology
- [ ] Validate API contracts with updated names
- [ ] Ensure backward compatibility during transition

### Validation Tests
- [ ] Confirm userOverrides flow works with interaction context
- [ ] Test agent validation pipeline with new terminology
- [ ] Verify audit logging captures correct entity names

---

## Rollback Plan

### If Issues Arise:
1. **Immediate**: Revert specific file changes causing build failures
2. **Short-term**: Maintain parallel terminology during transition
3. **Long-term**: Complete migration with proper testing

### Compatibility Considerations:
- Maintain interface compatibility during transition
- Use adapter patterns if needed for external integrations
- Document any breaking changes for dependent services

---

## Documentation Updates

### Completed 
- [x] Created EMMA-LEXICON.md with official terminology
- [x] Updated DATA_CONTRACT.md validation (already correct)
- [x] Created this migration guide

### Required 
- [ ] Update API documentation
- [ ] Review configuration management guide
- [ ] Update agent factory documentation
- [ ] Audit all markdown files for conversation references

---

## Success Criteria

### Phase 1: Core Services 
- [x] ContextProvider builds without errors
- [x] Intelligence data structures use flexible types
- [x] No more undefined type errors

### Phase 2: Interface Consistency
- [ ] All IContextProvider methods use interaction terminology
- [ ] Service implementations match interface contracts
- [ ] Zero build warnings related to terminology

### Phase 3: Full Migration
- [ ] All code uses consistent interaction terminology
- [ ] Documentation updated and consistent
- [ ] Tests pass with new terminology

---

## Notes

**Architectural Rationale**: 
- "Interaction" better represents discrete events in contact-centric architecture
- Aligns with existing data models and database schema
- Supports future AI/RAG workflows and agent coordination

**Risk Mitigation**:
- Incremental changes to avoid breaking builds
- Maintain backward compatibility where possible
- Comprehensive testing at each phase

**Next Steps**:
1. Complete ContextProvider build validation
2. Update IContextProvider interface
3. Migrate remaining service implementations
4. Update documentation and tests

---

**Status**: IN PROGRESS - Phase 1 Complete
**Last Updated**: 2024-06-10
**Next Review**: After Phase 2 completion
