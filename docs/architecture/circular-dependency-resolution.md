# Circular Dependency Resolution

## [2024-06-16] Moved Shared Interfaces to Emma.Models

- **What Changed:** Moved `IAgentAction`, `IAgentActionValidator`, and other shared interfaces from `Emma.Core` to `Emma.Models` to maintain a cleaner separation of concerns and prevent circular dependencies.

- **Why:** To keep all shared contracts and interfaces in a single, dedicated project that can be referenced by both `Emma.Core` and other projects without creating circular dependencies. This aligns with our architectural principle of keeping `Emma.Models` as the single source of truth for all shared contracts.

- **Guidelines/Verification Steps:**
  - All new shared interfaces should be added to `Emma.Models`
  - `Emma.Core` should only contain implementation-specific interfaces
  - Verify no direct references to `Emma.Core` exist in `Emma.Models`

- **Impact:** Developers must reference `Emma.Models` for all shared interfaces. The `Emma.Core` reference was removed from `Emma.Models` to prevent circular references.

## Problem

The EMMA solution had a circular dependency between `Emma.Core` and `Emma.Data` projects, which was causing build issues and architectural concerns. The main issues were:

1. `Emma.Data` referenced `Emma.Core` for some model classes and services
2. `Emma.Core` referenced `Emma.Data` for data access and model definitions
3. This created a tight coupling between the layers

## Solution

To resolve this, we implemented the following changes:

### 1. Created a New Shared Models Project

- Created `Emma.Models` project to contain all shared model classes and enums
- Moved all model classes from `Emma.Data/Models` to `Emma.Models/Models`
- Moved all enum types from `Emma.Data/Enums` to `Emma.Models/Enums`
- Moved validation attributes from `Emma.Data` to `Emma.Models.Validation`

### 2. Updated Project References

- Added project reference from `Emma.Data` to `Emma.Models`
- Added project reference from `Emma.Core` to `Emma.Models`
- Removed direct reference between `Emma.Data` and `Emma.Core`
- Updated `Emma.IntegrationTests` to reference both `Emma.Data` and `Emma.Core`
- Updated `Emma.Api` to reference both `Emma.Data` and `Emma.Core`

### 3. Resolved Circular References in Models

- Fixed circular reference between `SubscriptionPlan`, `SubscriptionPlanFeature`, and `Feature` classes
- Implemented proper navigation properties and foreign keys
- Added XML documentation for better code maintainability

### 4. Updated Namespaces and Using Directives

- Updated all namespaces from `Emma.Data.Models` to `Emma.Models.Models`
- Updated enum references to use `Emma.Models.Enums`
- Updated validation attributes to use `Emma.Models.Validation`

## Benefits

1. **Cleaner Architecture**: Clear separation of concerns between data access, business logic, and shared models
2. **Better Testability**: Models can be tested independently of data access or business logic
3. **Improved Maintainability**: Changes to models only require rebuilding dependent projects
4. **Reduced Coupling**: `Emma.Data` and `Emma.Core` are now properly decoupled
5. **Easier Refactoring**: Future architectural changes will be less likely to cause circular dependencies

## Migration Steps for New Models and Interfaces

When adding new models or interfaces:

1. **For Models:**
   - Add the model to the appropriate folder in `Emma.Models/Models/`
   - Add any required enums to `Emma.Models/Enums/`
   - Add validation attributes in `Emma.Models/Validation/` if needed
   - Reference the models from `Emma.Models` in both `Emma.Data` and `Emma.Core`

2. **For Shared Interfaces:**
   - Add new interfaces to `Emma.Models/Interfaces/`
   - Ensure interfaces don't contain implementation-specific types from `Emma.Core`
   - Use abstract classes or interfaces from `Emma.Models` when defining method parameters and return types
   - Keep interfaces focused on contracts, not implementations

## Verification

- All unit and integration tests are passing
- Solution builds successfully with no circular dependency warnings
- Static code analysis shows no circular references between projects
- `Emma.Models` has no dependencies on `Emma.Core`
- All shared interfaces are properly located in `Emma.Models`
- Existing functionality has been preserved with improved separation of concerns
