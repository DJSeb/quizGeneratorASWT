namespace FlakyTestDetector.Models;

/// <summary>
/// Represents the result of a single test execution
/// </summary>
public class TestResult
{
    public string TestName { get; set; } = string.Empty;
    public bool Passed { get; set; }
    public string? ErrorMessage { get; set; }
    public TimeSpan Duration { get; set; }
    public DateTime ExecutionTime { get; set; }
    public int ExecutionOrder { get; set; }
}
