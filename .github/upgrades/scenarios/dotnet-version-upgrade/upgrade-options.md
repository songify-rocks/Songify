# Upgrade Options Analysis

**Project**: Songify Slim  
**Current Framework**: .NET Framework 4.8  
**Target Framework**: .NET 10 (net10.0-windows)  
**Complexity**: 🔴 **HIGH**

## Executive Summary

Songify Slim is a WPF desktop application with 39K LOC requiring migration from .NET Framework 4.8 to .NET 10. The project uses legacy .csproj format and has significant but manageable compatibility challenges. **Full in-place upgrade is recommended** as the primary approach.

---

## Option Explanations

### Upgrade Strategy

Determines how projects are upgraded: all at once, bottom-up (dependencies first), or top-down (apps first).

### Project Approach (Framework Migration)

For .NET Framework projects moving to modern .NET, choose between:
- **In-place**: Upgrade existing projects directly
- **Side-by-side**: Scaffold new modern .NET projects alongside old ones, migrate incrementally

### SDK-Style Conversion

Convert legacy .csproj to SDK-style format (simpler, shorter, modern). Required for modern .NET.

### Package Management

Migrate from packages.config to PackageReference (modern, SDK-style required).

---

## Recommended Approach

| Option | **Selected Value** | Alternative Values | Notes |
|--------|-------------------|-------------------|-------|
| **Upgrade Strategy** | **All-at-Once** (selected) | Bottom-Up, Top-Down | Single project → simplest approach |
| **Project Approach** | **In-place** (selected) | Side-by-side | WPF is fully supported in .NET 10 |
| **SDK-Style Conversion** | **Yes** (selected) | - | Required for .NET 10 |
| **Package Management** | **PackageReference** (selected) | - | Required for SDK-style projects |

---

## Phases

### Phase 1: Project Structure Migration (Week 1)

**Tasks**:
1. Convert to SDK-style project
   - Target: `<TargetFramework>net10.0-windows</TargetFramework>`
   - Set: `<UseWPF>true</UseWPF>`
2. Migrate packages.config → PackageReference
3. Initial compilation test (expect ~8,111 errors)

**Deliverable**: SDK-style project targeting net10.0-windows

###  Phase 2: Dependency Resolution (Week 1-2)

**Tasks**:
4. Update compatible packages:
   - Microsoft.Extensions.* → 10.0.10
   - System.Text.Json → 10.0.10
5. Replace incompatible packages:
   - LiveCharts → LiveChartsCore 2.0+
   - WPF-UI → 2.0.3 or Wpf.Ui 3.0+
   - System.ServiceModel → CoreWCF 1.9.1
6. Remove redundant framework packages (40+ packages)

**Deliverable**: All packages compatible with .NET 10

### Phase 3: API Compatibility Fixes (Week 2-4)

**Tasks**:
7. Fix WPF binary incompatibilities (7,979 issues)
8. Address source incompatibilities (132 issues)
9. Review behavioral changes (197 issues)

**Deliverable**: Application compiles without errors

### Phase 4: Configuration & Testing (Week 4-5)

**Tasks**:
10. Resolve 7 binding redirect issues
11. Update app configuration
12. Comprehensive testing (unit, integration, UI workflows)

**Deliverable**: Fully validated application

### Phase 5: Deployment (Week 5-6)

**Tasks**:
13. Update build pipeline for .NET 10
14. Create deployment package

**Deliverable**: Production-ready .NET 10 application

---

## Critical Dependencies

### Must Replace

| From | To | Impact | Notes |
|------|-----|--------|-------|
| LiveCharts 0.9.7 | LiveChartsCore 2.0+ | HIGH | Breaking changes - chart code refactoring required |
| LiveCharts.Wpf 0.9.7 | LiveChartsCore.SkiaSharpView.WPF 2.0+ | HIGH | Breaking changes |
| System.ServiceModel.* | CoreWCF.* 1.9.1 | MEDIUM | API largely compatible |
| NHttp 0.1.9 | Alternative or remove | HIGH | Find replacement or remove feature |

### Must Update

| Package | From | To | Breaking |
|---------|------|-----|----------|
| WPF-UI | 4.3.0 | 2.0.3 | Yes (downgrade) |
| MahApps.Metro.IconPacks | 6.2.1 | 4.11.0 | Yes (downgrade) |
| Microsoft.Xaml.Behaviors.Wpf | 1.1.142 | 1.1.39 | No |

---

## Risk Assessment

| Risk | Impact | Probability | Mitigation |
|------|--------|-------------|------------|
| LiveCharts API breaking changes | HIGH | CERTAIN | Budget 1 week for chart refactoring |
| Binary incompatibility in UI code | HIGH | HIGH | Systematic testing after SDK conversion |
| Package incompatibilities | MEDIUM | MEDIUM | Documented alternatives exist |
| WCF service issues | HIGH | MEDIUM | CoreWCF is mature; test endpoints |
| Performance regression | MEDIUM | LOW | Baseline performance; .NET 10 typically faster |

---

## Estimated Timeline

- **Development**: 4-6 weeks
- **Testing**: 1-2 weeks
- **Total**: 5-8 weeks

**Technical Risk**: MEDIUM  
**Business Risk**: LOW-MEDIUM

---

## Success Criteria

- [ ] Application compiles without errors
- [ ] All features functionally equivalent
- [ ] Performance meets or exceeds Framework version
- [ ] No runtime errors in normal usage
- [ ] All integrations working (Spotify, Twitch, etc.)

---

## Alternative Options (Not Recommended)

### ⚠️ Incremental Multi-Targeting
**Why not**: Adds complexity without benefit for single-project app. Longer timeline (6-8 weeks).

### ❌ Complete Rewrite
**Why not**: WPF fully supported; 39K LOC investment; no architectural issues; 6-12 months timeline.

---

## Next Steps

1. **Immediate**: Review and approve this plan
2. **Week 1**: Convert to SDK-style, migrate packages
3. **Week 2-4**: Replace incompatible packages, fix errors
4. **Week 5-6**: Test, validate, deploy
