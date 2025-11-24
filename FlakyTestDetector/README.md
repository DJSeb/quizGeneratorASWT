# Flaky Test Detector for MSTest

## Overview

The Flaky Test Detector is a utility library designed to automatically detect flaky tests in MSTest-based C# projects. Flaky tests are tests that pass or fail non-deterministically due to external conditions, making them unreliable and difficult to debug.

## What are Flaky Tests?

Flaky tests exhibit inconsistent behavior:
- One run may pass, the next may fail (with no code changes)
- Results may depend on test execution order
- Behavior may vary based on timing, dates, or random values
- Tests may be affected by concurrent execution

Common causes include:
- **DateTime dependencies**: Using `DateTime.Now` or `DateTime.UtcNow`
- **Random values**: Using `Random` without a fixed seed
- **Timing issues**: Using `Thread.Sleep` or `Task.Delay`
- **Shared mutable state**: Static fields or shared resources
- **File system operations**: Improper cleanup or missing teardown
- **Non-deterministic values**: Using `Guid.NewGuid()`
- **Test order dependencies**: Tests that depend on execution order
- **Concurrency issues**: Race conditions in parallel execution

## Features

The Flaky Test Detector provides:

1. **Automatic Detection**: Runs tests multiple times to detect inconsistent behavior
2. **Multiple Detection Strategies**:
   - Sequential execution with repetition
   - Randomized test execution order
   - Parallel execution to detect concurrency issues
3. **Static Code Analysis**: Identifies common flaky test patterns in source code
4. **Root Cause Analysis**: Suggests potential causes for flakiness
5. **Repair Suggestions**: Provides actionable fixes for detected issues
6. **Detailed Reporting**: Generates comprehensive reports with statistics

## Installation

### Add the Library to Your Test Project

1. Add a project reference to `FlakyTestDetector.csproj`:
   ```bash
   dotnet add reference path/to/FlakyTestDetector/FlakyTestDetector.csproj
   ```

2. Or add the NuGet package (if published):
   ```bash
   dotnet add package FlakyTestDetector
   ```

## Usage

### Basic Usage

```csharp
using FlakyTestDetector;
using FlakyTestDetector.Models;
using System.Reflection;

// Configure the detector
var config = new DetectionConfig
{
    RepetitionCount = 10,            // Run each test 10 times
    ShuffleTestOrder = true,          // Randomize execution order
    EnableParallelExecution = true,   // Test for concurrency issues
    MaxDegreeOfParallelism = 4,      // Use 4 threads
    EnableStaticAnalysis = true,      // Analyze source code
    MinimumFlakinessThreshold = 0.1  // Report tests with >10% flakiness
};

// Create the runner
var runner = new FlakyTestRunner(config);

// Get your test assembly
var assembly = Assembly.GetExecutingAssembly();

// Run detection and print report
runner.RunAndPrintReport(assembly);
```

### Analyzing a Specific Test Class

```csharp
var runner = new FlakyTestRunner();
var reports = runner.RunDetection(typeof(MyTestClass));

foreach (var report in reports)
{
    Console.WriteLine($"Flaky Test: {report.TestName}");
    Console.WriteLine($"Flakiness: {report.FlakinessFactor:P1}");
    Console.WriteLine($"Passed: {report.PassedExecutions}/{report.TotalExecutions}");
}
```

### Saving Reports to File

```csharp
var runner = new FlakyTestRunner();
runner.RunAndSaveReport(assembly, "flaky-tests-report.txt");
```

## Configuration Options

### DetectionConfig Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `RepetitionCount` | int | 10 | Number of times to run each test |
| `ShuffleTestOrder` | bool | true | Whether to randomize test execution order |
| `EnableParallelExecution` | bool | true | Whether to run tests in parallel |
| `MaxDegreeOfParallelism` | int | 4 | Number of parallel threads |
| `EnableStaticAnalysis` | bool | true | Whether to analyze source code |
| `MinimumFlakinessThreshold` | double | 0.1 | Minimum flakiness to report (0.0-1.0) |

## Understanding the Reports

### Flakiness Factor

The flakiness factor is a value between 0.0 and 1.0:
- **0.0**: Test is consistent (always passes or always fails)
- **0.5**: Test is maximally flaky (50% pass, 50% fail)
- **1.0**: Theoretical maximum (not achievable in practice)

**Formula**: `min(passed, failed) / total_executions`

### Sample Report

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
  ✓ Passed:     6 (60.0%)
  ✗ Failed:     4 (40.0%)

Detected Issues:
  • Uses Random without explicit seed which can cause non-deterministic behavior

Suggested Fixes:
  → Use Random with a fixed seed (e.g., new Random(0)) for deterministic tests

Sample Failures:
  Run #3: Expected value < 5, got 7
  Run #5: Expected value < 5, got 8
  Run #9: Expected value < 5, got 6
```

## Example Flaky Test Scenarios

### 1. DateTime-Dependent Test

```csharp
[TestMethod]
public void Test_FailsAfterNoon()
{
    var now = DateTime.Now;
    Assert.IsTrue(now.Hour < 12, "Test must run before noon!");
}
```

**Fix**: Use dependency injection for time:
```csharp
[TestMethod]
public void Test_WithInjectedTime()
{
    var clock = new TestClock(new DateTime(2024, 1, 1, 10, 0, 0));
    Assert.IsTrue(clock.Now.Hour < 12);
}
```

### 2. Random Without Seed

```csharp
[TestMethod]
public void Test_RandomFailure()
{
    var random = new Random();
    var value = random.Next(0, 10);
    Assert.IsTrue(value < 5);
}
```

**Fix**: Use fixed seed:
```csharp
[TestMethod]
public void Test_DeterministicRandom()
{
    var random = new Random(42); // Fixed seed
    var value = random.Next(0, 10);
    Assert.AreEqual(6, value); // Always same value
}
```

### 3. Shared Mutable State

```csharp
[TestClass]
public class SharedStateFlakyTests
{
    private static int _counter = 0;

    [TestMethod]
    public void Test_IncrementCounter()
    {
        _counter++;
        Assert.AreEqual(1, _counter);
    }

    [TestMethod]
    public void Test_CounterIsZero()
    {
        Assert.AreEqual(0, _counter);
    }
}
```

**Fix**: Reset state in TestInitialize:
```csharp
[TestClass]
public class FixedTests
{
    private int _counter = 0; // Instance field

    [TestInitialize]
    public void Setup()
    {
        _counter = 0; // Reset before each test
    }
}
```

### 4. Concurrency Issues

```csharp
[TestMethod]
public void Test_ConcurrentIncrement()
{
    int counter = 0;
    Parallel.For(0, 100, i => counter++);
    Assert.AreEqual(100, counter); // May fail due to race condition
}
```

**Fix**: Use proper synchronization:
```csharp
[TestMethod]
public void Test_ThreadSafeIncrement()
{
    int counter = 0;
    var lockObj = new object();
    Parallel.For(0, 100, i => 
    {
        lock (lockObj) { counter++; }
    });
    Assert.AreEqual(100, counter);
}
```

## Running the Demo

The included demo project demonstrates various types of flaky tests:

```bash
cd FlakyTestDetector.Demo
dotnet run
```

This will:
1. Execute all demo tests multiple times
2. Detect which tests are flaky
3. Generate a detailed report
4. Save the report to `flaky-test-report.txt`

## Integration with CI/CD

### GitHub Actions

```yaml
name: Flaky Test Detection

on: [push, pull_request]

jobs:
  detect-flaky-tests:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v2
      - name: Setup .NET
        uses: actions/setup-dotnet@v1
        with:
          dotnet-version: 8.0.x
      - name: Run Flaky Test Detector
        run: |
          cd FlakyTestDetector.Demo
          dotnet run > flaky-report.txt
      - name: Upload Report
        uses: actions/upload-artifact@v2
        with:
          name: flaky-test-report
          path: flaky-report.txt
```

## Architecture

### Core Components

1. **Models**: Data structures for test results and reports
   - `TestResult`: Single test execution result
   - `FlakyTestReport`: Analysis results for a flaky test
   - `DetectionConfig`: Configuration options

2. **Detectors**: Test execution and detection logic
   - `FlakyTestDetector`: Runs tests multiple times with different conditions

3. **Analyzers**: Analysis and reporting
   - `StaticCodeAnalyzer`: Examines source code for common patterns
   - `ReportGenerator`: Creates human-readable reports

4. **FlakyTestRunner**: Main entry point and facade

## Limitations

- Static analysis requires source code access
- Cannot detect all types of flakiness (e.g., network issues)
- May have false positives for genuinely time-sensitive tests
- Performance: Running tests many times takes longer

## Best Practices

1. **Run regularly**: Integrate into CI/CD pipeline
2. **Start small**: Begin with high flakiness threshold
3. **Fix iteratively**: Address most flaky tests first
4. **Review suggestions**: Use as guidance, not gospel
5. **Update tests**: Keep tests deterministic and isolated

## Contributing

To extend the detector:
1. Add new pattern detection in `StaticCodeAnalyzer`
2. Add new execution strategies in `FlakyTestDetector`
3. Enhance reporting in `ReportGenerator`

## License

This project is part of the quizGeneratorASWT repository.

## References

- [Flaky Tests - JetBrains TeamCity](https://www.jetbrains.com/teamcity/ci-cd-guide/concepts/flaky-tests/)
- [The Ultimate Guide to Flaky Tests - Trunk.io](https://trunk.io/blog/the-ultimate-guide-to-flaky-tests)
- [An Empirical Analysis of Flaky Tests - FSE 2014](https://mir.cs.illinois.edu/marinov/publications/LuoETAL14FlakyTestsAnalysis.pdf)
- [What is a Flaky Test? - GeeksforGeeks](https://www.geeksforgeeks.org/what-is-a-flaky-test/)
