# EMMA Build Debug Workflow

## Purpose
Stepwise debugging approach for EMMA builds to prevent overwhelming Cascade memory and ensure systematic problem resolution.

## Workflow Steps

### Step 1: Initial Build Assessment
- Run initial build command: `dotnet build`
- Capture and analyze ONLY the first 3-5 build errors
- Do NOT attempt to fix all errors at once
- Create memory of error patterns and root causes

### Step 2: Categorize Errors
- Group errors by type:
  - **Compilation errors** (syntax, missing references)
  - **Dependency errors** (NuGet packages, project references)
  - **Configuration errors** (appsettings, connection strings)
  - **Architecture errors** (interface mismatches, breaking changes)

### Step 3: Priority-Based Fixing
- Fix errors in this order:
  1. **Critical blockers** (missing dependencies, major syntax)
  2. **Architecture issues** (interface changes, breaking changes)
  3. **Configuration issues** (settings, connections)
  4. **Minor compilation** (warnings, style issues)

### Step 4: Incremental Validation
- After fixing 3-5 errors, run build again
- Verify fixes didn't introduce new issues
- Document what was fixed and why
- Create checkpoint memory if significant progress made

### Step 5: Memory Management
- Create focused memories for:
  - Root cause patterns
  - Successful fix strategies
  - Architecture decisions made
  - Configuration changes applied
- Keep memories specific and actionable

### Step 6: Iteration Control
- If more than 10 errors remain, STOP and create checkpoint
- If build succeeds, run basic smoke tests
- If new errors appear, categorize and prioritize again
- Never attempt to fix more than 5 errors per iteration

## Key Principles

### Memory Conservation
- Focus on one error category at a time
- Create targeted memories, not comprehensive ones
- Use checkpoints for complex debugging sessions
- Avoid overwhelming context with all errors at once

### Systematic Approach
- Always run build between fix attempts
- Document the reasoning behind each fix
- Maintain EMMA architecture consistency
- Preserve interaction terminology standards

### Quality Gates
- Verify each fix doesn't break existing functionality
- Ensure Responsible AI compliance maintained
- Check that logging and error handling remain intact
- Validate database migrations still work

## Usage
Invoke with: `/emma-build-debug`

## Example Invocation
```
I'm getting build errors in EMMA. Let's use the stepwise debug approach:
/emma-build-debug
```

## Success Criteria
- Build completes successfully
- No new errors introduced
- Architecture consistency maintained
- Memory usage kept manageable
- Progress documented in memories
