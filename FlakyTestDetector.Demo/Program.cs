using FlakyTestDetector;
using FlakyTestDetector.Models;
using System.Reflection;

namespace FlakyTestDetector.Demo;

/// <summary>
/// Demo program that runs the flaky test detector
/// </summary>
public class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine("╔══════════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║     Flaky Test Detector Demo - MSTest Extension                 ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════════╝");
        Console.WriteLine();

        // Configure the detector
        var config = new DetectionConfig
        {
            RepetitionCount = 5,           // Run each test 5 times
            ShuffleTestOrder = true,        // Shuffle test execution order
            EnableParallelExecution = true, // Run tests in parallel to catch concurrency issues
            MaxDegreeOfParallelism = 2,    // Use 2 threads for parallel execution
            EnableStaticAnalysis = true,    // Enable static code analysis
            MinimumFlakinessThreshold = 0.0 // Report all flaky tests
        };

        // Create the runner
        var runner = new FlakyTestRunner(config);

        // Get the current assembly (contains the demo tests)
        var assembly = Assembly.GetExecutingAssembly();

        Console.WriteLine("Running flaky test detection on demo test suite...");
        Console.WriteLine($"Test Assembly: {assembly.GetName().Name}");
        Console.WriteLine();

        // Run detection and print report
        runner.RunAndPrintReport(assembly);

        // Also save report to file
        var reportPath = Path.Combine(Directory.GetCurrentDirectory(), "flaky-test-report.txt");
        var reports = runner.RunDetection(assembly);
        var reportGenerator = new Analyzers.ReportGenerator();
        reportGenerator.SaveReportToFile(reports, reportPath);
        
        Console.WriteLine();
        Console.WriteLine($"📄 Full report saved to: {reportPath}");
        Console.WriteLine();
        
        // Print summary statistics
        if (reports.Any())
        {
            Console.WriteLine("Summary Statistics:");
            Console.WriteLine($"  Total Flaky Tests: {reports.Count}");
            Console.WriteLine($"  Most Flaky Test: {reports.OrderByDescending(r => r.FlakinessFactor).First().TestName}");
            Console.WriteLine($"  Average Flakiness: {reports.Average(r => r.FlakinessFactor):P1}");
        }

        Console.WriteLine();
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }
}
