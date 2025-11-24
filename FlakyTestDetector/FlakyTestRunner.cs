using FlakyTestDetector.Analyzers;
using FlakyTestDetector.Models;
using System.Reflection;

namespace FlakyTestDetector;

/// <summary>
/// Main entry point for flaky test detection
/// </summary>
public class FlakyTestRunner
{
    private readonly DetectionConfig _config;
    private readonly Detectors.FlakyTestDetector _detector;
    private readonly StaticCodeAnalyzer _staticAnalyzer;
    private readonly ReportGenerator _reportGenerator;

    public FlakyTestRunner(DetectionConfig? config = null)
    {
        _config = config ?? new DetectionConfig();
        _detector = new Detectors.FlakyTestDetector(_config);
        _staticAnalyzer = new StaticCodeAnalyzer();
        _reportGenerator = new ReportGenerator();
    }

    /// <summary>
    /// Runs flaky test detection on a test assembly
    /// </summary>
    public List<FlakyTestReport> RunDetection(Assembly testAssembly)
    {
        Console.WriteLine("Starting flaky test detection...");
        Console.WriteLine($"Configuration: {_config.RepetitionCount} repetitions, " +
                         $"Shuffle: {_config.ShuffleTestOrder}, " +
                         $"Parallel: {_config.EnableParallelExecution}");
        Console.WriteLine();

        var reports = _detector.DetectFlakyTests(testAssembly);

        // Enhance reports with static analysis if enabled
        if (_config.EnableStaticAnalysis)
        {
            EnhanceWithStaticAnalysis(reports, testAssembly);
        }

        return reports;
    }

    /// <summary>
    /// Runs flaky test detection on a specific test class
    /// </summary>
    public List<FlakyTestReport> RunDetection(Type testClass)
    {
        Console.WriteLine($"Starting flaky test detection for {testClass.Name}...");
        Console.WriteLine($"Configuration: {_config.RepetitionCount} repetitions, " +
                         $"Shuffle: {_config.ShuffleTestOrder}, " +
                         $"Parallel: {_config.EnableParallelExecution}");
        Console.WriteLine();

        var reports = _detector.DetectFlakyTests(testClass);

        // Enhance reports with static analysis if enabled
        if (_config.EnableStaticAnalysis)
        {
            EnhanceWithStaticAnalysis(reports, testClass.Assembly);
        }

        return reports;
    }

    /// <summary>
    /// Runs detection and prints the report to console
    /// </summary>
    public void RunAndPrintReport(Assembly testAssembly)
    {
        var reports = RunDetection(testAssembly);
        var report = _reportGenerator.GenerateReport(reports);
        Console.WriteLine(report);
    }

    /// <summary>
    /// Runs detection and prints the report to console for a specific test class
    /// </summary>
    public void RunAndPrintReport(Type testClass)
    {
        var reports = RunDetection(testClass);
        var report = _reportGenerator.GenerateReport(reports);
        Console.WriteLine(report);
    }

    /// <summary>
    /// Runs detection and saves the report to a file
    /// </summary>
    public void RunAndSaveReport(Assembly testAssembly, string outputPath)
    {
        var reports = RunDetection(testAssembly);
        _reportGenerator.SaveReportToFile(reports, outputPath);
        Console.WriteLine($"Report saved to: {outputPath}");
    }

    private void EnhanceWithStaticAnalysis(List<FlakyTestReport> reports, Assembly assembly)
    {
        foreach (var report in reports)
        {
            try
            {
                // Try to get source code for analysis
                var typeName = report.TestName.Substring(0, report.TestName.LastIndexOf('.'));
                var methodName = report.TestName.Substring(report.TestName.LastIndexOf('.') + 1);

                var type = assembly.GetTypes().FirstOrDefault(t => t.Name == typeName);
                if (type != null)
                {
                    var method = type.GetMethod(methodName);
                    if (method != null)
                    {
                        // Get source file location and content
                        var sourceFilePath = TryGetSourceFilePath(type);
                        if (!string.IsNullOrEmpty(sourceFilePath) && File.Exists(sourceFilePath))
                        {
                            var sourceCode = File.ReadAllText(sourceFilePath);
                            var issues = _staticAnalyzer.AnalyzeTestMethod(sourceCode, methodName);
                            report.DetectedIssues.AddRange(issues);
                            report.SuggestedFixes.AddRange(_staticAnalyzer.SuggestFixes(issues));
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Static analysis is best-effort - if we can't load source files or parse them,
                // we simply skip static analysis for this test and continue with behavioral detection
            }
        }
    }

    private string? TryGetSourceFilePath(Type type)
    {
        // Try to infer source file path from assembly location
        var assemblyPath = type.Assembly.Location;
        if (string.IsNullOrEmpty(assemblyPath))
            return null;

        var assemblyDir = Path.GetDirectoryName(assemblyPath);
        if (string.IsNullOrEmpty(assemblyDir))
            return null;

        // Navigate up to find the project root
        var projectDir = FindProjectRoot(assemblyDir);
        if (projectDir == null)
            return null;

        // Search for the source file
        var fileName = $"{type.Name}.cs";
        var files = Directory.GetFiles(projectDir, fileName, SearchOption.AllDirectories);
        return files.FirstOrDefault();
    }

    private string? FindProjectRoot(string startPath)
    {
        var dir = new DirectoryInfo(startPath);
        while (dir != null)
        {
            if (Directory.GetFiles(dir.FullName, "*.csproj").Any())
            {
                return dir.FullName;
            }
            dir = dir.Parent;
        }
        return null;
    }
}
