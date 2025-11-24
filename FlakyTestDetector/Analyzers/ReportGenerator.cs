using FlakyTestDetector.Models;
using System.Text;

namespace FlakyTestDetector.Analyzers;

/// <summary>
/// Generates human-readable reports for flaky tests
/// </summary>
public class ReportGenerator
{
    public string GenerateReport(List<FlakyTestReport> flakyTests)
    {
        var sb = new StringBuilder();

        sb.AppendLine("╔══════════════════════════════════════════════════════════════════╗");
        sb.AppendLine("║           FLAKY TEST DETECTION REPORT                            ║");
        sb.AppendLine("╚══════════════════════════════════════════════════════════════════╝");
        sb.AppendLine();

        if (flakyTests.Count == 0)
        {
            sb.AppendLine("✓ No flaky tests detected!");
            sb.AppendLine();
            sb.AppendLine("All tests executed consistently across multiple runs.");
            return sb.ToString();
        }

        sb.AppendLine($"⚠ Found {flakyTests.Count} flaky test(s)");
        sb.AppendLine();

        // Sort by flakiness factor (most flaky first)
        var sortedTests = flakyTests.OrderByDescending(t => t.FlakinessFactor).ToList();

        foreach (var test in sortedTests)
        {
            sb.AppendLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            sb.AppendLine($"Test: {test.TestName}");
            sb.AppendLine($"Flakiness Factor: {test.FlakinessFactor:P1} ({test.FlakinessFactor:F3})");
            sb.AppendLine();

            sb.AppendLine("Execution Summary:");
            sb.AppendLine($"  Total Runs:   {test.TotalExecutions}");
            sb.AppendLine($"  ✓ Passed:     {test.PassedExecutions} ({(test.PassedExecutions / (double)test.TotalExecutions):P1})");
            sb.AppendLine($"  ✗ Failed:     {test.FailedExecutions} ({(test.FailedExecutions / (double)test.TotalExecutions):P1})");
            sb.AppendLine();

            if (test.DetectedIssues.Any())
            {
                sb.AppendLine("Detected Issues:");
                foreach (var issue in test.DetectedIssues)
                {
                    sb.AppendLine($"  • {issue}");
                }
                sb.AppendLine();
            }

            if (test.SuggestedFixes.Any())
            {
                sb.AppendLine("Suggested Fixes:");
                foreach (var fix in test.SuggestedFixes)
                {
                    sb.AppendLine($"  → {fix}");
                }
                sb.AppendLine();
            }

            // Show first few failures
            var failures = test.ExecutionHistory.Where(r => !r.Passed).Take(3).ToList();
            if (failures.Any())
            {
                sb.AppendLine("Sample Failures:");
                foreach (var failure in failures)
                {
                    sb.AppendLine($"  Run #{failure.ExecutionOrder}: {failure.ErrorMessage}");
                }
                sb.AppendLine();
            }
        }

        sb.AppendLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        sb.AppendLine();
        sb.AppendLine("Recommendations:");
        sb.AppendLine("  1. Review and fix the most flaky tests first (highest flakiness factor)");
        sb.AppendLine("  2. Ensure tests are independent and don't share mutable state");
        sb.AppendLine("  3. Use deterministic values for dates, random numbers, and GUIDs");
        sb.AppendLine("  4. Avoid timing-based waits; use proper synchronization");
        sb.AppendLine("  5. Clean up resources properly in TestCleanup methods");
        sb.AppendLine();

        return sb.ToString();
    }

    public string GenerateSummary(List<FlakyTestReport> flakyTests)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Flaky Test Detection Summary:");
        sb.AppendLine($"  Total Flaky Tests: {flakyTests.Count}");
        
        if (flakyTests.Any())
        {
            sb.AppendLine($"  Average Flakiness: {flakyTests.Average(t => t.FlakinessFactor):P1}");
            sb.AppendLine($"  Most Flaky: {flakyTests.OrderByDescending(t => t.FlakinessFactor).First().TestName}");
        }

        return sb.ToString();
    }

    public void SaveReportToFile(List<FlakyTestReport> flakyTests, string filePath)
    {
        var report = GenerateReport(flakyTests);
        File.WriteAllText(filePath, report);
    }
}
