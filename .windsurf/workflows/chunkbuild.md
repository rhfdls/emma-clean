# EMMA Build Error Chunking Workflow

Process EMMA build errors in small, manageable chunks (5-10 errors at a time) to avoid overwhelming Cascade with massive build logs.

## Step 1: Build Assessment
Run build and capture current error count:
```bash
dotnet build Emma.Core 2>&1 | findstr /C:"error" | measure-object -line
```

## Step 2: Extract Error Chunk
Extract first 5-10 errors for focused analysis:
```bash
dotnet build Emma.Core 2>&1 | findstr /C:"error" | select-object -first 10
```

## Step 3: Categorize Errors
Classify errors by type:
- **Category A (HIGH)**: Interface/model mismatches (AgentContext, AgentRequest, Contact, AgentResponse)
- **Category B (MEDIUM)**: Missing fields (NbaRecommendation, ValidationResult, ApprovalStatus)  
- **Category C (MEDIUM)**: Type conversions (UrgencyLevel, logging parameters)
- **Category D (LOW)**: Missing variables (NbaAgent fields)

## Step 4: Fix Current Chunk
Address 5-10 errors systematically:
1. Identify root cause for each error
2. Make targeted fixes without breaking existing functionality
3. Verify fixes don't introduce new errors

## Step 5: Validate Progress
Confirm error reduction:
```bash
dotnet build Emma.Core 2>&1 | findstr /C:"error" | measure-object -line
```

## Step 6: Document Progress
Update error count and categories in memory:
- Starting errors: [INITIAL_COUNT]
- Current errors: [CURRENT_COUNT]
- Fixed errors: [FIXED_COUNT]
- Remaining categories: [CATEGORIES]

## Step 7: Repeat Process
Continue with next chunk until all errors resolved.

## Step 8: Final Validation
Run complete build to ensure success:
```bash
dotnet build Emma.Core
```

## Success Criteria
- Reduce total error count by 5-10 per iteration
- Maintain existing functionality
- No new compilation errors introduced
- Clear progress tracking and documentation
