# Flaky Test Detector - Quick Start Guide

This repository now includes a **Flaky Test Detector** - an MSTest extension that automatically detects flaky tests in your test suites.

## What is This?

The Flaky Test Detector analyzes your MSTest test suite by:
- Running tests multiple times with different conditions
- Detecting inconsistent pass/fail behavior
- Identifying root causes through static code analysis
- Suggesting fixes for common flaky test patterns

## Running the Demo

To see the detector in action with example flaky tests:

```bash
cd FlakyTestDetector.Demo
dotnet run
```

This will execute the demo test suite and generate a detailed report showing detected flaky tests.

## Using in Your Own Tests

### 1. Add Reference to Your Test Project

```bash
dotnet add reference path/to/FlakyTestDetector/FlakyTestDetector.csproj
```

### 2. Create a Detection Runner

```csharp
using FlakyTestDetector;
using FlakyTestDetector.Models;
using System.Reflection;

// Configure detection
var config = new DetectionConfig
{
    RepetitionCount = 10,           // Run each test 10 times
    EnableParallelExecution = true, // Test concurrency issues
    EnableStaticAnalysis = true     // Analyze source code
};

// Run detection
var runner = new FlakyTestRunner(config);
var assembly = Assembly.GetExecutingAssembly();
runner.RunAndPrintReport(assembly);
```

### 3. Interpret Results

The detector reports a **flakiness factor** (0.0 to 1.0) for each inconsistent test:
- **0.4-0.5**: Highly flaky (passes ~50% of the time)
- **0.2-0.4**: Moderately flaky
- **0.1-0.2**: Mildly flaky
- **< 0.1**: Marginally flaky

## Common Flaky Test Patterns Detected

| Pattern | Detection Method | Suggested Fix |
|---------|------------------|---------------|
| `DateTime.Now` usage | Static analysis | Use injected time provider |
| `Random` without seed | Static analysis | Use `new Random(fixedSeed)` |
| `Thread.Sleep` / `Task.Delay` | Static analysis | Use proper synchronization |
| Shared static fields | Static analysis | Reset in `[TestInitialize]` |
| `Guid.NewGuid()` | Static analysis | Use deterministic GUIDs |
| File system operations | Static analysis | Clean up in `[TestCleanup]` |
| Test order dependency | Repeated execution | Make tests independent |
| Race conditions | Parallel execution | Use thread-safe code |

## Documentation

For detailed documentation, see [FlakyTestDetector/README.md](FlakyTestDetector/README.md)

## Example Output

```
╔══════════════════════════════════════════════════════════════════╗
║           FLAKY TEST DETECTION REPORT                            ║
╚══════════════════════════════════════════════════════════════════╝

⚠ Found 2 flaky test(s)

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Test: RandomFlakyTests.Test_RandomFailure
Flakiness Factor: 40.0% (0.400)

Execution Summary:
  Total Runs:   10
  ✓ Passed:     4 (40.0%)
  ✗ Failed:     6 (60.0%)

Detected Issues:
  • Uses Random without explicit seed which can cause non-deterministic behavior

Suggested Fixes:
  → Use Random with a fixed seed (e.g., new Random(0)) for deterministic tests
```

## Project Structure

- **FlakyTestDetector/** - Core library
  - `Models/` - Data models (TestResult, FlakyTestReport, DetectionConfig)
  - `Detectors/` - Test execution and flakiness detection
  - `Analyzers/` - Static code analysis and reporting
- **FlakyTestDetector.Demo/** - Example flaky tests and demo application

## Integration with CI/CD

You can integrate the detector into your CI/CD pipeline to catch flaky tests early. See the main [README](FlakyTestDetector/README.md) for examples with GitHub Actions and other CI systems.
