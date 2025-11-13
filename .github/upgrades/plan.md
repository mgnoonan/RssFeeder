# NuGet Package Dependency Update Plan

## Executive Summary

### Scenario
Update outdated NuGet packages across the RssFeeder solution to their latest stable versions, improving security, performance, and compatibility with .NET 10.0.

### Scope
- **Total Projects**: 3 projects
- **Packages to Update**: 20 packages (MediatR excluded per user request)
- **Current State**: All projects targeting .NET 10.0, no security vulnerabilities detected

### Target State
All packages updated to their latest stable versions while maintaining compatibility with .NET 10.0 and existing functionality.

### Selected Strategy
**Big Bang Strategy** - Update all packages simultaneously across all projects in a single coordinated operation.

### Strategy Rationale
- Small solution (3 projects) with simple structure
- No complex interdependencies between package updates
- All packages have minor or patch version updates (except MediatR which is excluded)
- Good test coverage expected in web application
- Faster completion with single validation cycle

### Complexity Assessment
**Low Complexity** - Most updates are patch or minor versions with minimal breaking changes expected.

### Critical Issues
- ✅ **No security vulnerabilities** detected in any packages
- ⚠️ **MediatR excluded** - User requested to skip MediatR upgrade (12.5.0 → 13.1.0) due to potential breaking changes
- ℹ️ **Microsoft.Extensions.*** packages have major version updates (9.0.10 → 10.0.0) but align with .NET 10.0

### Recommended Approach
Big Bang approach with all packages updated simultaneously, followed by comprehensive build and test validation.

---

## Migration Strategy

### 2.1 Approach Selection

**Chosen Strategy**: Big Bang Strategy

**Justification**:
- **Small solution**: Only 3 projects total
- **Simple dependency structure**: RssFeeder.Models is a leaf node with 1 package, other projects are independent
- **Low-risk updates**: Most are patch/minor version updates
- **No security vulnerabilities**: Updates are for compatibility and features, not critical security fixes
- **Efficient execution**: Single update cycle is faster than incremental approach

### 2.2 Dependency-Based Ordering

**Project Dependency Structure**:
```
RssFeeder.Models (leaf - no dependencies)
    ↑
    └── RssFeeder.Console (depends on RssFeeder.Models)

RssFeeder.Mvc (independent - no project dependencies)
```

**Big Bang Execution Order**:
Since we're using Big Bang strategy, all projects will be updated simultaneously. However, the natural validation order follows dependency structure:
1. **RssFeeder.Models** - Update first (leaf node, simple validation)
2. **RssFeeder.Console & RssFeeder.Mvc** - Update in parallel (no interdependency)

### 2.3 Parallel vs Sequential Execution

**Strategy Considerations**: 
Big Bang strategy dictates all package updates happen atomically across all projects. The projects will be updated simultaneously in a single operation, with validation happening after all updates are complete.

**Parallelization**: 
Within the atomic update, RssFeeder.Console and RssFeeder.Mvc can be conceptually parallel since they have no interdependencies.

---

## Detailed Dependency Analysis

### 3.1 Dependency Graph Summary

**Migration Phases (Big Bang Strategy)**:
- **Phase 0**: Preparation - Backup and branch verification
- **Phase 1**: Atomic Update - All package updates across all projects simultaneously
- **Phase 2**: Validation - Build, test, and verify all projects together

### 3.2 Project Groupings

**Phase 1: Atomic Package Update (All Projects Simultaneously)**

**Group A: Foundation Library**
- RssFeeder.Models
  - 0 packages to update (Newtonsoft.Json is compatible, no update needed)

**Group B: Application Projects (Updated in parallel)**
- RssFeeder.Console
  - 13 packages to update
- RssFeeder.Mvc  
  - 5 packages to update (MediatR excluded)

---

## Project-by-Project Migration Plans

### Project: RssFeeder.Models

**Current State**
- Target Framework: net10.0
- Package Count: 1
- LOC: 111
- Dependencies: 0 project dependencies
- Dependants: RssFeeder.Console

**Target State**
- Target Framework: net10.0 (unchanged)
- Updated Packages: 0 (Newtonsoft.Json 13.0.4 is compatible)

**Migration Steps**

1. **Prerequisites**
   - None - project has no updates required

2. **Package Updates**
   - No package updates required for this project

3. **Expected Breaking Changes**
   - None

4. **Code Modifications**
   - None required

5. **Testing Strategy**
   - Build verification only
   - Verify RssFeeder.Console still builds correctly with this project reference

6. **Validation Checklist**
   - [x] Project builds without errors
   - [x] Project builds without warnings
   - [x] Dependent project (RssFeeder.Console) still builds

---

### Project: RssFeeder.Console

**Current State**
- Target Framework: net10.0
- Package Count: 30 packages
- Dependencies: RssFeeder.Models (project reference)
- Package Updates Available: 13

**Target State**
- Target Framework: net10.0 (unchanged)
- Updated Packages: 13

**Migration Steps**

1. **Prerequisites**
   - RssFeeder.Models project builds successfully (no changes required)

2. **Framework Update**
   - No target framework changes required

3. **Package Updates**

| Package | Current Version | Target Version | Update Type | Reason |
|---------|----------------|----------------|-------------|---------|
| AngleSharp | 1.3.0 | 1.3.1 | Patch | Bug fixes and improvements |
| Microsoft.Azure.Cosmos | 3.54.0 | 3.54.1 | Patch | Bug fixes |
| Microsoft.Extensions.Configuration | 9.0.10 | 10.0.0 | Major | .NET 10.0 alignment |
| Microsoft.Extensions.Configuration.Binder | 9.0.10 | 10.0.0 | Major | .NET 10.0 alignment |
| Microsoft.Extensions.Configuration.EnvironmentVariables | 9.0.10 | 10.0.0 | Major | .NET 10.0 alignment |
| Microsoft.Extensions.Configuration.FileExtensions | 9.0.10 | 10.0.0 | Major | .NET 10.0 alignment |
| Microsoft.Extensions.Configuration.Json | 9.0.10 | 10.0.0 | Major | .NET 10.0 alignment |
| Microsoft.Extensions.Configuration.UserSecrets | 9.0.10 | 10.0.0 | Major | .NET 10.0 alignment |
| Microsoft.Extensions.FileProviders.Physical | 9.0.10 | 10.0.0 | Major | .NET 10.0 alignment |
| Microsoft.Extensions.Hosting.Abstractions | 9.0.10 | 10.0.0 | Major | .NET 10.0 alignment |
| Microsoft.Extensions.Options.ConfigurationExtensions | 9.0.10 | 10.0.0 | Major | .NET 10.0 alignment |
| Selenium.Support | 4.37.0 | 4.38.0 | Minor | New features and fixes |
| Selenium.WebDriver | 4.37.0 | 4.38.0 | Minor | New features and fixes |
| Serilog.Sinks.Console | 6.0.0 | 6.1.1 | Minor | Improvements |
| System.Linq.Dynamic.Core | 1.6.9 | 1.6.10 | Patch | Bug fixes |
| System.ServiceModel.Syndication | 9.0.10 | 10.0.0 | Major | .NET 10.0 alignment |

4. **Expected Breaking Changes**

**Microsoft.Extensions.* packages (9.x → 10.x)**
- These packages align with .NET 10.0 and should have minimal breaking changes
- Configuration APIs are generally stable across versions
- Potential issues:
  - Configuration binding behavior changes (unlikely but possible)
  - Obsolete API removals (if any deprecated APIs were used)

**Selenium packages (4.37 → 4.38)**
- Minor version update, minimal breaking changes expected
- WebDriver API is generally stable
- May include new features and bug fixes

**Other packages (patch updates)**
- AngleSharp, Azure.Cosmos, Serilog, System.Linq.Dynamic.Core: Patch updates with minimal risk

5. **Code Modifications**

Expected modifications based on package updates:
- **Configuration**: Review configuration setup in `Program.cs` for any obsolete patterns
- **Selenium**: Verify WebDriver initialization and usage patterns still work
- **No code changes expected** for other packages (patch updates)

Areas requiring review:
- `Program.cs` - Configuration bootstrapping
- `WebUtils.cs` - Web scraping with Selenium
- `WebCrawler.cs` - HTTP client usage

6. **Testing Strategy**

**Unit Tests**:
- Execute all unit tests (if present)
- Focus on configuration loading and parsing logic

**Integration Tests**:
- Test web scraping functionality with Selenium
- Verify RSS feed parsing and article extraction
- Test all feed builders (Drudge, WhatFinger, Conservagator, etc.)

**Manual Testing**:
- Run console application with various feed configurations
- Verify article parsing from different sources
- Check Selenium-based crawling still works

**Performance Validation**:
- Verify crawling performance hasn't degraded
- Check memory usage with new Selenium version

7. **Validation Checklist**
   - [ ] Dependencies resolve correctly
   - [ ] Project builds without errors
   - [ ] Project builds without warnings (or only expected NU1510 warnings about unnecessary packages)
   - [ ] All unit tests pass
   - [ ] Integration tests pass (feed parsing, web scraping)
   - [ ] Console application runs successfully
   - [ ] No regression in feed processing functionality

---

### Project: RssFeeder.Mvc

**Current State**
- Target Framework: net10.0
- Package Count: 17 packages
- LOC: 1,085
- Project Kind: ASP.NET Core Razor Pages
- Dependencies: 0 project dependencies

**Target State**
- Target Framework: net10.0 (unchanged)
- Updated Packages: 5 (MediatR excluded)

**Migration Steps**

1. **Prerequisites**
   - None - project is independent

2. **Framework Update**
   - No target framework changes required

3. **Package Updates**

| Package | Current Version | Target Version | Update Type | Reason |
|---------|----------------|----------------|-------------|---------|
| Microsoft.Azure.Cosmos | 3.54.0 | 3.54.1 | Patch | Bug fixes |
| Microsoft.Identity.Web | 4.0.0 | 4.0.1 | Patch | Bug fixes and security improvements |
| Microsoft.Identity.Web.UI | 4.0.0 | 4.0.1 | Patch | Bug fixes and security improvements |
| Serilog.Sinks.ApplicationInsights | 4.0.0 | 4.1.0 | Minor | New features |
| Serilog.Sinks.Console | 6.0.0 | 6.1.1 | Minor | Improvements |

**Excluded Package**:
- ~~MediatR: 12.5.0 → 13.1.0~~ (Excluded per user request due to potential breaking changes in major version)

4. **Expected Breaking Changes**

**Microsoft.Identity.Web packages (4.0.0 → 4.0.1)**
- Patch update, minimal risk
- May include security fixes for authentication
- Authentication middleware should work without changes

**Serilog packages (minor updates)**
- Serilog.Sinks.ApplicationInsights (4.0 → 4.1): Minor version, backward compatible
- Serilog.Sinks.Console (6.0 → 6.1.1): Minor version, backward compatible

**Microsoft.Azure.Cosmos (3.54.0 → 3.54.1)**
- Patch update, minimal risk

**No breaking changes expected** for these updates.

5. **Code Modifications**

Expected modifications:
- **No code changes expected** - all updates are patch or minor versions
- **Authentication**: Verify authentication flow still works (Microsoft.Identity.Web)
- **Logging**: Verify Application Insights logging still works

Areas requiring review:
- `Program.cs` / `Startup.cs` - Authentication and logging configuration
- `Views/Home/Index.cshtml` - Razor page functionality
- Any controllers or pages using authentication attributes

6. **Testing Strategy**

**Unit Tests**:
- Execute all unit tests (if present)
- Focus on business logic and data access

**Integration Tests**:
- Test authentication flow (login/logout)
- Verify Cosmos DB access still works
- Test Application Insights telemetry

**Manual Testing**:
- Start the web application
- Test user authentication (if applicable)
- Verify RSS feed display functionality
- Check Application Insights logging in Azure portal
- Test all Razor pages for functionality

**Performance Validation**:
- Verify page load times haven't degraded
- Check Application Insights telemetry is still captured

7. **Validation Checklist**
   - [ ] Dependencies resolve correctly
   - [ ] Project builds without errors
   - [ ] Project builds without warnings (except expected NU1510 for System.Text.RegularExpressions)
   - [ ] All unit tests pass
   - [ ] Web application starts successfully
   - [ ] Authentication works correctly
   - [ ] Cosmos DB connectivity works
   - [ ] Application Insights logging works
   - [ ] All Razor pages render correctly
   - [ ] No regression in RSS feed display

---

## Package Update Reference

### Common Package Updates (Affecting Multiple Projects)

| Package | Current | Target | Projects Affected | Update Reason | Risk Level |
|---------|---------|--------|-------------------|---------------|------------|
| Microsoft.Azure.Cosmos | 3.54.0 | 3.54.1 | RssFeeder.Console, RssFeeder.Mvc | Bug fixes | Low |
| Serilog.Sinks.Console | 6.0.0 | 6.1.1 | RssFeeder.Console, RssFeeder.Mvc | Improvements | Low |

### RssFeeder.Console-Specific Updates

| Package | Current | Target | Update Reason | Risk Level |
|---------|---------|--------|---------------|------------|
| AngleSharp | 1.3.0 | 1.3.1 | Bug fixes | Low |
| Microsoft.Extensions.Configuration | 9.0.10 | 10.0.0 | .NET 10.0 alignment | Medium |
| Microsoft.Extensions.Configuration.Binder | 9.0.10 | 10.0.0 | .NET 10.0 alignment | Medium |
| Microsoft.Extensions.Configuration.EnvironmentVariables | 9.0.10 | 10.0.0 | .NET 10.0 alignment | Medium |
| Microsoft.Extensions.Configuration.FileExtensions | 9.0.10 | 10.0.0 | .NET 10.0 alignment | Medium |
| Microsoft.Extensions.Configuration.Json | 9.0.10 | 10.0.0 | .NET 10.0 alignment | Medium |
| Microsoft.Extensions.Configuration.UserSecrets | 9.0.10 | 10.0.0 | .NET 10.0 alignment | Medium |
| Microsoft.Extensions.FileProviders.Physical | 9.0.10 | 10.0.0 | .NET 10.0 alignment | Medium |
| Microsoft.Extensions.Hosting.Abstractions | 9.0.10 | 10.0.0 | .NET 10.0 alignment | Medium |
| Microsoft.Extensions.Options.ConfigurationExtensions | 9.0.10 | 10.0.0 | .NET 10.0 alignment | Medium |
| Selenium.Support | 4.37.0 | 4.38.0 | New features and fixes | Low |
| Selenium.WebDriver | 4.37.0 | 4.38.0 | New features and fixes | Low |
| System.Linq.Dynamic.Core | 1.6.9 | 1.6.10 | Bug fixes | Low |
| System.ServiceModel.Syndication | 9.0.10 | 10.0.0 | .NET 10.0 alignment | Medium |

### RssFeeder.Mvc-Specific Updates

| Package | Current | Target | Update Reason | Risk Level |
|---------|---------|--------|---------------|------------|
| Microsoft.Identity.Web | 4.0.0 | 4.0.1 | Security and bug fixes | Low |
| Microsoft.Identity.Web.UI | 4.0.0 | 4.0.1 | Security and bug fixes | Low |
| Serilog.Sinks.ApplicationInsights | 4.0.0 | 4.1.0 | New features | Low |

### Excluded Packages

| Package | Current | Available | Project | Reason for Exclusion |
|---------|---------|-----------|---------|---------------------|
| MediatR | 12.5.0 | 13.1.0 | RssFeeder.Mvc | User requested exclusion - major version update with potential breaking changes |

---

## Risk Management

### 5.1 High-Risk Changes

**Strategy Risk Factors**: Big Bang strategy means all changes happen at once, increasing the blast radius if issues occur. Mitigation: thorough testing after atomic update.

| Project/Package | Risk | Mitigation |
|----------------|------|------------|
| Microsoft.Extensions.* (9.x → 10.x) | Configuration behavior changes | Test configuration loading thoroughly; verify appsettings.json and user secrets work correctly |
| Selenium packages (4.37 → 4.38) | WebDriver API changes | Test all web scraping scenarios; verify ChromeDriver/EdgeDriver compatibility |
| All packages simultaneously | Multiple changes at once increases debugging complexity | Maintain detailed change log; ability to rollback entire update atomically |

### 5.2 Risk Assessment by Package

**Low Risk (Patch Updates)**:
- AngleSharp 1.3.0 → 1.3.1
- Microsoft.Azure.Cosmos 3.54.0 → 3.54.1
- Microsoft.Identity.Web* 4.0.0 → 4.0.1
- System.Linq.Dynamic.Core 1.6.9 → 1.6.10

**Medium Risk (Minor/Major Updates)**:
- Microsoft.Extensions.* 9.0.10 → 10.0.0 (major version, but aligns with .NET 10)
- Selenium.* 4.37.0 → 4.38.0 (minor version)
- Serilog.Sinks.* (minor versions)
- System.ServiceModel.Syndication 9.0.10 → 10.0.0 (major version, but aligns with .NET 10)

**High Risk (Excluded)**:
- MediatR 12.5.0 → 13.1.0 (major version with known breaking changes) - **EXCLUDED**

### 5.3 Contingency Plans

**Big Bang Strategy Challenges**:

If the atomic update fails or introduces critical issues:

**Scenario 1: Build Failures After Update**
- Rollback: Revert all package updates using Git (`git checkout -- *.csproj`)
- Alternative: Restore from backup of project files
- Debug: Identify specific package causing failure, update others individually

**Scenario 2: Runtime Errors After Update**
- Rollback: Full rollback to previous branch state
- Alternative: Incremental testing - update packages in smaller groups:
  - Group 1: Microsoft.Extensions.* packages only
  - Group 2: Selenium packages
  - Group 3: Remaining packages

**Scenario 3: Authentication Issues (Microsoft.Identity.Web)**
- Rollback: Revert Microsoft.Identity.Web packages only
- Debug: Check authentication middleware configuration
- Alternative: Stay on 4.0.0 temporarily, update separately later

**Scenario 4: Web Scraping Failures (Selenium)**
- Rollback: Revert Selenium packages to 4.37.0
- Debug: Verify ChromeDriver/EdgeDriver versions are compatible
- Alternative: Update WebDriver binaries separately

**Scenario 5: Configuration Loading Issues**
- Rollback: Revert Microsoft.Extensions.Configuration.* packages
- Debug: Test configuration loading in isolation
- Alternative: Use compatibility shims if available

---

## Testing and Validation Strategy

### 6.1 Phase-by-Phase Testing

**Big Bang Strategy Testing Approach**:

**Phase 0: Pre-Update Baseline**
- Run all tests and record baseline results
- Document current functionality
- Create backup/tag in Git

**Phase 1: Post-Update Validation** (after atomic update)
- Immediate build verification (all projects)
- Dependency resolution check
- Warning/error review

**Phase 2: Comprehensive Testing**
- Unit tests (all projects)
- Integration tests (all projects)
- Manual smoke tests (both applications)

### 6.2 Smoke Tests

**RssFeeder.Console Smoke Tests**:
1. Application starts without errors
2. Configuration loads successfully
3. Can connect to data sources (Cosmos DB, RavenDB)
4. Feed parsing works for at least one feed source
5. Web scraping with Selenium works
6. Article extraction with parsers works
7. Logging to Seq/file works

**RssFeeder.Mvc Smoke Tests**:
1. Web application starts without errors
2. Home page loads successfully
3. Authentication works (if applicable)
4. Cosmos DB connectivity works
5. RSS feed display works
6. Application Insights telemetry flows
7. Logging works correctly

### 6.3 Comprehensive Validation

**Before marking update complete:**

**Build Validation**:
- [ ] All projects build without errors
- [ ] No new warnings introduced (except known NU1510 warnings)
- [ ] NuGet package restore succeeds
- [ ] Dependency graph is valid

**Automated Test Validation**:
- [ ] All unit tests pass (if present)
- [ ] All integration tests pass (if present)
- [ ] No test timeouts or flakiness introduced

**Functional Validation**:
- [ ] RssFeeder.Console runs successfully with multiple feed configurations
- [ ] All feed builders work (Drudge, WhatFinger, Conservagator, FreedomPress, BonginoReport, etc.)
- [ ] All tag parsers work (Generic, JsonLd, Script, Html)
- [ ] Selenium web scraping works correctly
- [ ] RssFeeder.Mvc web application starts and serves pages
- [ ] Authentication flow works (if applicable)
- [ ] RSS feed display in web UI works

**Integration Validation**:
- [ ] Cosmos DB connectivity works in both projects
- [ ] RavenDB connectivity works (Console)
- [ ] Application Insights telemetry works (Mvc)
- [ ] Seq logging works (both projects)

**Performance Validation**:
- [ ] Feed parsing performance is acceptable
- [ ] Web scraping performance hasn't degraded
- [ ] Web page load times are acceptable
- [ ] No memory leaks detected

**Security Validation**:
- [ ] No new security warnings from `dotnet list package --vulnerable`
- [ ] Authentication still secure (Mvc)
- [ ] No new deprecation warnings

---

## Timeline and Effort Estimates

### 7.1 Per-Project Estimates

**Big Bang Strategy - Atomic Update Approach**

| Phase | Complexity | Estimated Time | Tasks | Risk Level |
|-------|------------|---------------|-------|------------|
| Phase 0: Preparation | Low | 15 minutes | Backup, branch verification | Low |
| Phase 1: Atomic Update | Low | 30 minutes | Update all 20 packages across 2 projects simultaneously, restore, build | Medium |
| Phase 2: Validation & Testing | Medium | 2-3 hours | Build verification, unit tests, integration tests, smoke tests | Medium |
| **Total** | **Low-Medium** | **3-4 hours** | **All phases** | **Medium** |

### 7.2 Phase Durations

**Phase 0: Preparation** (15 minutes)
- Verify clean Git state
- Create backup/tag
- Confirm on correct branch (dependency-updates-assessment)

**Phase 1: Atomic Package Update** (30 minutes)
- Update all packages in RssFeeder.Console simultaneously
- Update all packages in RssFeeder.Mvc simultaneously  
- Run `dotnet restore` on solution
- Run `dotnet build` on solution
- Address any immediate build errors

**Phase 2: Comprehensive Validation** (2-3 hours)
- Unit test execution: 30 minutes
- Integration test execution: 30 minutes
- Console application smoke testing: 30 minutes
- Web application smoke testing: 30 minutes
- Performance validation: 30 minutes
- Documentation update: 30 minutes

**Buffer**: +1 hour for unexpected issues

**Total Estimated Time**: **3-4 hours** (with buffer: 4-5 hours)

### 7.3 Resource Requirements

**Skills Needed**:
- .NET/C# development experience
- Familiarity with NuGet package management
- ASP.NET Core Razor Pages knowledge
- Understanding of Selenium WebDriver
- Experience with Serilog and Application Insights

**Parallel Work Capacity**:
- Big Bang strategy means updates happen atomically
- Validation can be parallelized:
  - One developer: Console application testing
  - Another developer: Web application testing
  - Can reduce validation time to 1-2 hours with 2 developers

**Testing Resources**:
- Access to Cosmos DB instance
- Access to RavenDB instance (for Console)
- Access to Azure Application Insights (for Mvc)
- Access to Seq logging server (if applicable)
- Web browsers for Selenium testing

---

## Source Control Strategy

### 8.1 Strategy-Specific Guidance

**Big Bang Strategy Source Control Approach**:
- **Single Commit Preferred**: Since all packages update atomically, use a single commit after successful validation
- **Branch**: Already on `dependency-updates-assessment` branch
- **Commit Message Template**: "chore: update 20 NuGet packages to latest versions (exclude MediatR)"

### 8.2 Branching Strategy

**Current Branch**: `dependency-updates-assessment` (already created)

**Branch Strategy**:
- ✅ Main branch: `master`
- ✅ Update branch: `dependency-updates-assessment` (active)
- No feature branches needed (Big Bang atomic update)

**Integration Approach**:
- After successful validation, merge `dependency-updates-assessment` → `master`
- Use pull request for code review if team process requires it
- Delete feature branch after merge

### 8.3 Commit Strategy

**Big Bang Strategy Commit Approach**:

**Option 1: Single Atomic Commit (Recommended)**
```
chore: update 20 NuGet packages to latest versions

- Update Microsoft.Extensions.* packages (9.0.10 → 10.0.0) - 9 packages
- Update Selenium packages (4.37.0 → 4.38.0) - 2 packages
- Update Serilog.Sinks.Console (6.0.0 → 6.1.1) - 2 projects
- Update Microsoft.Identity.Web packages (4.0.0 → 4.0.1) - 2 packages
- Update Microsoft.Azure.Cosmos (3.54.0 → 3.54.1) - 2 projects
- Update AngleSharp (1.3.0 → 1.3.1)
- Update System.Linq.Dynamic.Core (1.6.9 → 1.6.10)
- Update System.ServiceModel.Syndication (9.0.10 → 10.0.0)
- Update Serilog.Sinks.ApplicationInsights (4.0.0 → 4.1.0)

Excluded: MediatR (12.5.0 → 13.1.0) per team decision

All tests passing. No breaking changes detected.
```

**Option 2: Two Commits (If validation requires intermediate checkpoint)**
```
Commit 1: Update all package versions
Commit 2: Fix any breaking changes (if needed)
```

**Checkpoint Strategy**:
- Create Git tag before starting: `git tag pre-package-update`
- Single commit after successful validation
- Tag after completion: `git tag package-update-complete`

### 8.4 Review and Merge Process

**Pull Request Requirements**:
- **Title**: "Update NuGet packages to latest versions (20 packages)"
- **Description**: Include package update summary and test results
- **Reviewers**: Assign team member(s) if required
- **Checks**: Ensure CI/CD pipeline passes (if configured)

**Review Checklist**:
- [ ] All package versions updated as planned
- [ ] MediatR correctly excluded from updates
- [ ] No unintended changes to project files
- [ ] Build succeeds
- [ ] Tests pass
- [ ] No new security vulnerabilities

**Merge Criteria**:
- All tests passing
- No regression in functionality
- Code review approved (if required)
- CI/CD pipeline green (if configured)

**Integration Validation**:
- Merge to `master` branch
- Verify `master` builds successfully
- Tag release if appropriate: `v1.x.x-package-updates`

---

## Success Criteria

### 9.1 Strategy-Specific Success Criteria

**Big Bang Strategy Success Indicators**:
- ✅ All 20 packages updated simultaneously in single operation
- ✅ Solution builds successfully after atomic update
- ✅ All tests pass in first validation cycle
- ✅ No rollback required
- ✅ Single commit/PR captures all changes

### 9.2 Technical Success Criteria

**Package Update Success**:
- [ ] All 20 planned packages updated to target versions
- [ ] MediatR correctly remains at version 12.5.0 (excluded)
- [ ] No unintended package updates
- [ ] Package restore succeeds without errors
- [ ] Dependency resolution is clean

**Build Success**:
- [ ] RssFeeder.Models builds without errors
- [ ] RssFeeder.Console builds without errors
- [ ] RssFeeder.Mvc builds without errors
- [ ] Solution builds without errors
- [ ] Only expected warnings present (NU1510 warnings acceptable)

**Functionality Success**:
- [ ] RssFeeder.Console application runs successfully
- [ ] All feed builders work correctly
- [ ] All tag parsers work correctly
- [ ] Selenium web scraping works
- [ ] RssFeeder.Mvc web application starts and serves pages
- [ ] Authentication works (if applicable)
- [ ] Database connectivity works (Cosmos DB, RavenDB)

**Testing Success**:
- [ ] All unit tests pass (if present)
- [ ] All integration tests pass (if present)
- [ ] Manual smoke tests pass
- [ ] No performance regression detected

**Security Success**:
- [ ] No security vulnerabilities in dependencies (`dotnet list package --vulnerable` clean)
- [ ] No deprecated packages in use
- [ ] Authentication security maintained

### 9.3 Quality Criteria

**Code Quality**:
- [ ] No new compiler warnings introduced
- [ ] Code style/formatting maintained
- [ ] No obsolete API usage warnings

**Documentation**:
- [ ] This plan document completed
- [ ] Package update notes documented
- [ ] Any code changes documented in commit message

**Process Quality**:
- [ ] Big Bang strategy principles followed
- [ ] Single atomic commit created
- [ ] Proper branch management followed
- [ ] Pull request created with clear description

### 9.4 Acceptance Criteria

**Deployment Readiness**:
- [ ] All success criteria above met
- [ ] Code review completed (if required)
- [ ] CI/CD pipeline passes
- [ ] Team approval obtained

**Final Verification**:
- [ ] Run final `dotnet list package --outdated` to confirm updates applied
- [ ] Run final `dotnet list package --vulnerable` to confirm no vulnerabilities
- [ ] Verify both applications work in deployment environment (if possible)

---

## Additional Notes

### Warnings to Expect

**NU1510 Warnings (Expected and Acceptable)**:
The following warnings will appear but are informational only (packages may be unnecessary but aren't causing issues):

**RssFeeder.Console**:
- Microsoft.Extensions.Configuration
- Microsoft.Extensions.Configuration.Binder
- Microsoft.Extensions.Configuration.EnvironmentVariables
- Microsoft.Extensions.Configuration.FileExtensions
- Microsoft.Extensions.Configuration.Json
- Microsoft.Extensions.Configuration.UserSecrets
- Microsoft.Extensions.FileProviders.Physical
- Microsoft.Extensions.Hosting.Abstractions
- Microsoft.Extensions.Options.ConfigurationExtensions

**RssFeeder.Mvc**:
- System.Text.RegularExpressions

**Action**: These warnings can be addressed in a future cleanup task by removing unnecessary packages, but they don't block this update.

### Post-Update Cleanup Opportunities

After successful package updates, consider these follow-up tasks:

1. **Remove Unnecessary Packages**: Address NU1510 warnings by removing packages that are likely unnecessary
2. **MediatR Update**: Plan future update to MediatR 13.x with proper testing and code changes
3. **Newtonsoft.Json Migration**: Consider scenario "NewtonSoftJsonToSystemTextJson" for future modernization
4. **Package Consolidation**: Review if Directory.Build.props could centralize common package versions

### Breaking Changes Resources

If breaking changes are encountered, consult these resources:

**Microsoft.Extensions.* (9.x → 10.x)**:
- [.NET 10.0 Breaking Changes](https://learn.microsoft.com/en-us/dotnet/core/compatibility/10.0)
- [ASP.NET Core 10.0 Breaking Changes](https://learn.microsoft.com/en-us/aspnet/core/migration/90-to-100)

**Selenium (4.37 → 4.38)**:
- [Selenium Changelog](https://github.com/SeleniumHQ/selenium/blob/trunk/dotnet/CHANGELOG)

**Serilog**:
- Generally backward compatible, check specific sink documentation if issues arise

---

## Rollback Plan

### Quick Rollback (if needed immediately)

**Option 1: Git Revert (Preferred for Big Bang)**
```bash
# If update was committed
git revert HEAD

# If update not yet committed  
git checkout -- RssFeeder.Console/RssFeeder.Console.csproj
git checkout -- RssFeeder.Mvc/RssFeeder.Mvc.csproj
```

**Option 2: Branch Rollback**
```bash
# Switch back to master
git checkout master

# Delete update branch
git branch -D dependency-updates-assessment
```

**Option 3: Restore from Backup**
- Restore project files from pre-update backup
- Run `dotnet restore` to restore old packages

### Partial Rollback (if specific packages cause issues)

If only certain packages cause problems, can selectively revert:

**Revert Microsoft.Extensions.* packages**:
```xml
<!-- Change back to 9.0.10 in RssFeeder.Console.csproj -->
<PackageReference Include="Microsoft.Extensions.Configuration" Version="9.0.10" />
<!-- ... repeat for other Microsoft.Extensions.* packages ... -->
```

**Revert Selenium packages**:
```xml
<PackageReference Include="Selenium.Support" Version="4.37.0" />
<PackageReference Include="Selenium.WebDriver" Version="4.37.0" />
```

After selective rollback:
```bash
dotnet restore
dotnet build
# Re-test affected functionality
```

---

## Appendix: Package Update Commands

### Manual Update Commands (for reference)

**RssFeeder.Console**:
```bash
cd RssFeeder.Console
dotnet add package AngleSharp --version 1.3.1
dotnet add package Microsoft.Azure.Cosmos --version 3.54.1
dotnet add package Microsoft.Extensions.Configuration --version 10.0.0
dotnet add package Microsoft.Extensions.Configuration.Binder --version 10.0.0
dotnet add package Microsoft.Extensions.Configuration.EnvironmentVariables --version 10.0.0
dotnet add package Microsoft.Extensions.Configuration.FileExtensions --version 10.0.0
dotnet add package Microsoft.Extensions.Configuration.Json --version 10.0.0
dotnet add package Microsoft.Extensions.Configuration.UserSecrets --version 10.0.0
dotnet add package Microsoft.Extensions.FileProviders.Physical --version 10.0.0
dotnet add package Microsoft.Extensions.Hosting.Abstractions --version 10.0.0
dotnet add package Microsoft.Extensions.Options.ConfigurationExtensions --version 10.0.0
dotnet add package Selenium.Support --version 4.38.0
dotnet add package Selenium.WebDriver --version 4.38.0
dotnet add package Serilog.Sinks.Console --version 6.1.1
dotnet add package System.Linq.Dynamic.Core --version 1.6.10
dotnet add package System.ServiceModel.Syndication --version 10.0.0
```

**RssFeeder.Mvc**:
```bash
cd RssFeeder.Mvc
dotnet add package Microsoft.Azure.Cosmos --version 3.54.1
dotnet add package Microsoft.Identity.Web --version 4.0.1
dotnet add package Microsoft.Identity.Web.UI --version 4.0.1
dotnet add package Serilog.Sinks.ApplicationInsights --version 4.1.0
dotnet add package Serilog.Sinks.Console --version 6.1.1
```

### Verification Commands

```bash
# Verify package versions updated
dotnet list package

# Check for outdated packages
dotnet list package --outdated

# Check for vulnerabilities
dotnet list package --vulnerable

# Restore packages
dotnet restore

# Build solution
dotnet build

# Run tests (if configured)
dotnet test
```

---

## Summary

This plan outlines updating 20 NuGet packages across 2 projects (RssFeeder.Models has no updates) using a **Big Bang Strategy** approach. All packages will be updated simultaneously in a single atomic operation, with MediatR excluded per user request.

**Key Points**:
- ✅ No security vulnerabilities in current or target packages
- ✅ Most updates are low-risk patch or minor versions
- ⚠️ Microsoft.Extensions.* packages have major version updates (9.x → 10.x) but align with .NET 10.0
- ⚠️ MediatR excluded from update (stays at 12.5.0)
- ⏱️ Estimated completion time: 3-4 hours with testing
- 🎯 Big Bang strategy: All updates happen atomically, single commit

**Next Steps**:
1. Review and approve this plan
2. Execute Phase 0: Preparation
3. Execute Phase 1: Atomic package update
4. Execute Phase 2: Comprehensive validation
5. Commit changes and create pull request
6. Merge to master after approval

---

*Plan generated: 2025*
*Target .NET Version: 10.0*
*Strategy: Big Bang*
*Total Packages to Update: 20 (MediatR excluded)*