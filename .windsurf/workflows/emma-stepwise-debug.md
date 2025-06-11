# EMMA Stepwise Debug Master Workflow

## Purpose
Master workflow for systematic, memory-efficient debugging of EMMA builds and tests using a stepwise approach.

## Workflow Overview

### Phase 1: Build Stabilization
Use `/emma-build-debug` workflow:
1. Fix compilation errors in small batches (3-5 at a time)
2. Prioritize critical blockers and architecture issues
3. Create focused memories for error patterns
4. Validate incrementally to prevent regression

### Phase 2: Test Validation  
Use `/emma-test-debug` workflow:
1. Run tests in categories (Unit → Integration → Configuration)
2. Fix test failures in small batches (3-5 at a time)
3. Maintain test independence and coverage
4. Document test fix strategies in memories

### Phase 3: Integration Verification
1. Run full build and test suite together
2. Verify no cross-dependencies or conflicts
3. Check EMMA architecture compliance
4. Validate interaction terminology consistency

## Memory Management Strategy

### Checkpoint Creation
Create checkpoints when:
- Completing a major error category (5+ fixes)
- Switching between build and test phases
- Making significant architecture decisions
- Encountering complex debugging scenarios

### Memory Content Focus
- **Root causes** not just symptoms
- **Successful strategies** for similar issues
- **Architecture decisions** and their rationale
- **Configuration patterns** that work

## Quality Gates

### Build Quality
- ✅ No compilation errors
- ✅ All dependencies resolved
- ✅ Configuration valid
- ✅ Architecture consistency maintained

### Test Quality  
- ✅ Critical business logic tests pass
- ✅ Integration tests validate real scenarios
- ✅ Test coverage maintained
- ✅ No test interdependencies

### EMMA Standards
- ✅ Interaction terminology used consistently
- ✅ Responsible AI compliance maintained
- ✅ Logging and error handling intact
- ✅ Database migrations functional

## Usage Patterns

### For Build Issues:
```
EMMA build is failing. Let's debug step by step:
/emma-stepwise-debug

Starting with build phase...
```

### For Test Issues:
```
EMMA tests failing after refactoring. Using stepwise approach:
/emma-stepwise-debug

Build is clean, moving to test phase...
```

### For Full Debug Session:
```
EMMA has multiple build and test issues. Full stepwise debug:
/emma-stepwise-debug

Will work through build → test → integration phases systematically.
```

## Success Metrics
- **Memory Efficiency**: No overwhelming context, focused memories
- **Problem Resolution**: Systematic fix of root causes
- **Architecture Integrity**: EMMA standards maintained
- **Development Velocity**: Faster debugging through consistent approach
- **Knowledge Retention**: Reusable patterns captured in memories

## Anti-Patterns to Avoid
- ❌ Attempting to fix all errors at once
- ❌ Creating overly broad, unfocused memories  
- ❌ Skipping incremental validation steps
- ❌ Ignoring architecture consistency checks
- ❌ Mixing build and test fixes in same iteration
