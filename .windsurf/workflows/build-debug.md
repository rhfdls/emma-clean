# EMMA Build Debug Workflow

Systematic approach to fixing all build errors in the EMMA project using prioritized error categorization.

## Overview
This workflow addresses 109+ build errors across 4 priority categories using a systematic approach that tackles the most critical interface/model mismatches first, then works through missing properties, type conversions, and finally missing constants.

## Steps

### Step 1: Build Assessment
Run initial build to capture current error state and categorize issues.

```bash
dotnet build --no-restore --verbosity normal > build-errors.log 2>&1
```

**Success Criteria**: Complete error log captured with categorization

### Step 2: Category A - Critical Interface/Model Mismatches (HIGH PRIORITY)
Fix the most critical errors that prevent compilation:

1. **AgentContext Interface Issues** - Missing properties (AgentType, Capabilities, Configuration, State, Metrics, AuditId, Reason)
2. **AgentRequest Missing Properties** - OrganizationId, Parameters, Message  
3. **Contact Missing OrganizationId** - Multiple service references failing
4. **AgentResponse Generic Issues** - Cannot use with type arguments

**Success Criteria**: Interface/model alignment completed, Category A errors eliminated

### Step 3: Category B - Missing Fields/Properties (MEDIUM PRIORITY)
Address missing properties in core models:

1. **NbaRecommendation Missing** - Timing, ExpectedOutcome properties
2. **ValidationResult Missing IsValid** property
3. **EnumConfigurationChangedEventArgs Missing** - ChangeType, ChangedBy, Description, Timestamp
4. **ApprovalStatus Missing** - Status, SubmittedBy, SubmittedAt, RequiredApprovers

**Success Criteria**: All missing properties added, Category B errors resolved

### Step 4: Category C - Type Conversion Errors (MEDIUM PRIORITY)
Fix type casting and conversion issues:

1. **UrgencyLevel Enum Casting** - int to UrgencyLevel conversions
2. **Logging Parameter Mismatches** - EventId and parameter order issues
3. **Industry Profile Interface** - Missing ResourceTypes, DefaultResourceCategories

**Success Criteria**: Type conversion errors resolved, proper casting implemented

### Step 5: Category D - Missing Variables/Constants (LOW PRIORITY)
Add missing fields and constants:

1. **NbaAgent Missing Fields** - _rateLimitTracker, _requestCounts, MaxRequestsPerMinute, MaxRequestsPerHour

**Success Criteria**: All missing variables/constants added

### Step 6: Incremental Build Verification
Run build after each category to verify progress:

```bash
dotnet build --no-restore
```

**Success Criteria**: Error count decreases after each category completion

### Step 7: Final Build Validation
Execute final clean build to confirm all errors resolved:

```bash
dotnet clean
dotnet build
```

**Success Criteria**: Zero build errors, successful compilation

### Step 8: Documentation Update
Update build status and document any remaining warnings or technical debt.

**Success Criteria**: Build status documented, workflow completion recorded

## Error Categories Summary

- **Category A (HIGH)**: Interface/model mismatches - 25+ errors
- **Category B (MEDIUM)**: Missing fields/properties - 35+ errors  
- **Category C (MEDIUM)**: Type conversions - 30+ errors
- **Category D (LOW)**: Missing variables/constants - 19+ errors

## Expected Outcomes

- **Zero build errors** across all projects
- **Successful compilation** of Emma.Core, Emma.Data, Emma.Api
- **Maintained architecture** integrity during fixes
- **Documented resolution** of all critical issues
