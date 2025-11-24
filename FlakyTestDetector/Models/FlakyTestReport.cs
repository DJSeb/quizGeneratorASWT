namespace FlakyTestDetector.Models;

/// <summary>
/// Represents a flaky test with analysis results
/// </summary>
public class FlakyTestReport
{
    public string TestName { get; set; } = string.Empty;
    public int TotalExecutions { get; set; }
    public int PassedExecutions { get; set; }
    public int FailedExecutions { get; set; }
    public double FlakinessFactor { get; set; } // 0.0 to 1.0, where 1.0 is most flaky
    public List<string> DetectedIssues { get; set; } = new();
    public List<string> SuggestedFixes { get; set; } = new();
    public List<TestResult> ExecutionHistory { get; set; } = new();
}
