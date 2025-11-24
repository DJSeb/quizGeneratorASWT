namespace FlakyTestDetector.Models;

/// <summary>
/// Configuration for flaky test detection
/// </summary>
public class DetectionConfig
{
    /// <summary>
    /// Number of times to run each test to detect flakiness
    /// </summary>
    public int RepetitionCount { get; set; } = 10;

    /// <summary>
    /// Whether to shuffle test execution order
    /// </summary>
    public bool ShuffleTestOrder { get; set; } = true;

    /// <summary>
    /// Whether to run tests in parallel to detect concurrency issues
    /// </summary>
    public bool EnableParallelExecution { get; set; } = true;

    /// <summary>
    /// Degree of parallelism when parallel execution is enabled
    /// </summary>
    public int MaxDegreeOfParallelism { get; set; } = 4;

    /// <summary>
    /// Whether to perform static code analysis for common flaky patterns
    /// </summary>
    public bool EnableStaticAnalysis { get; set; } = true;

    /// <summary>
    /// Minimum flakiness factor to report (0.0-1.0)
    /// </summary>
    public double MinimumFlakinessThreshold { get; set; } = 0.1;
}
