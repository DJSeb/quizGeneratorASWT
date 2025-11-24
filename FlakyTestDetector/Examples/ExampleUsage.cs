using FlakyTestDetector;
using FlakyTestDetector.Models;
using System.Reflection;

namespace FlakyTestDetector.Examples;

/// <summary>
/// Example showing how to use the Flaky Test Detector on an existing test project
/// </summary>
public class ExampleUsage
{
    /// <summary>
    /// Basic usage - detect flaky tests in an assembly
    /// </summary>
    public static void BasicUsage()
    {
        // Create default configuration
        var runner = new FlakyTestRunner();
        
        // Get your test assembly
        var assembly = Assembly.GetExecutingAssembly();
        
        // Run and print report
        runner.RunAndPrintReport(assembly);
    }

    /// <summary>
    /// Advanced usage with custom configuration
    /// </summary>
    public static void AdvancedUsage()
    {
        var config = new DetectionConfig
        {
            RepetitionCount = 20,              // Run each test 20 times
            ShuffleTestOrder = true,            // Detect test order dependencies
            EnableParallelExecution = true,     // Detect concurrency issues
            MaxDegreeOfParallelism = 8,        // Use 8 threads
            EnableStaticAnalysis = true,        // Analyze source code
            MinimumFlakinessThreshold = 0.05   // Report tests with >5% flakiness
        };

        var runner = new FlakyTestRunner(config);
        var assembly = Assembly.GetExecutingAssembly();
        
        // Get detailed reports
        var reports = runner.RunDetection(assembly);
        
        // Process results
        foreach (var report in reports.OrderByDescending(r => r.FlakinessFactor))
        {
            Console.WriteLine($"Test: {report.TestName}");
            Console.WriteLine($"Flakiness: {report.FlakinessFactor:P2}");
            Console.WriteLine($"Issues: {string.Join(", ", report.DetectedIssues)}");
            Console.WriteLine();
        }
    }

    /// <summary>
    /// Analyze a specific test class
    /// </summary>
    public static void AnalyzeSpecificClass<T>() where T : class
    {
        var runner = new FlakyTestRunner();
        var reports = runner.RunDetection(typeof(T));
        
        if (reports.Any())
        {
            Console.WriteLine($"Found {reports.Count} flaky test(s) in {typeof(T).Name}");
            foreach (var report in reports)
            {
                Console.WriteLine($"  - {report.TestName} ({report.FlakinessFactor:P1})");
            }
        }
        else
        {
            Console.WriteLine($"No flaky tests found in {typeof(T).Name}");
        }
    }

    /// <summary>
    /// Save report to file for CI/CD integration
    /// </summary>
    public static void SaveReportForCI()
    {
        var config = new DetectionConfig
        {
            RepetitionCount = 10,
            EnableParallelExecution = true,
            MinimumFlakinessThreshold = 0.1
        };

        var runner = new FlakyTestRunner(config);
        var assembly = Assembly.GetExecutingAssembly();
        
        // Run detection and save to file
        var outputPath = Path.Combine(
            Environment.GetEnvironmentVariable("BUILD_ARTIFACTSTAGINGDIRECTORY") ?? ".",
            "flaky-test-report.txt"
        );
        
        runner.RunAndSaveReport(assembly, outputPath);
        
        // Optionally fail the build if flaky tests are found
        var reports = runner.RunDetection(assembly);
        if (reports.Any())
        {
            Console.Error.WriteLine($"ERROR: Found {reports.Count} flaky test(s)!");
            Environment.Exit(1);
        }
    }

    /// <summary>
    /// Quick check - just detect and return count
    /// </summary>
    public static int QuickCheck()
    {
        var config = new DetectionConfig
        {
            RepetitionCount = 5,  // Faster for quick checks
            EnableStaticAnalysis = false
        };

        var runner = new FlakyTestRunner(config);
        var assembly = Assembly.GetExecutingAssembly();
        var reports = runner.RunDetection(assembly);
        
        return reports.Count;
    }

    /// <summary>
    /// Focused detection for specific issues
    /// </summary>
    public static void FocusedDetection()
    {
        // Focus on detecting test order dependencies
        var config = new DetectionConfig
        {
            RepetitionCount = 15,
            ShuffleTestOrder = true,
            EnableParallelExecution = false,  // Sequential only
            EnableStaticAnalysis = true
        };

        var runner = new FlakyTestRunner(config);
        var assembly = Assembly.GetExecutingAssembly();
        var reports = runner.RunDetection(assembly);
        
        var orderDependentTests = reports
            .Where(r => r.DetectedIssues.Any(i => i.Contains("static") || i.Contains("state")))
            .ToList();

        Console.WriteLine($"Found {orderDependentTests.Count} test(s) with potential order dependencies");
    }

    /// <summary>
    /// Performance-focused detection (faster execution)
    /// </summary>
    public static void FastDetection()
    {
        var config = new DetectionConfig
        {
            RepetitionCount = 3,               // Minimal repetition
            ShuffleTestOrder = false,          // No shuffling
            EnableParallelExecution = false,    // Sequential only
            EnableStaticAnalysis = true,        // Static analysis is fast
            MinimumFlakinessThreshold = 0.2    // Higher threshold
        };

        var runner = new FlakyTestRunner(config);
        var assembly = Assembly.GetExecutingAssembly();
        runner.RunAndPrintReport(assembly);
    }
}
