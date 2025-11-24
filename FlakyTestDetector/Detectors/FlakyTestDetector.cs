using FlakyTestDetector.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;

namespace FlakyTestDetector.Detectors;

/// <summary>
/// Core detector that executes tests multiple times to detect flaky behavior
/// </summary>
public class FlakyTestDetector
{
    private readonly DetectionConfig _config;

    public FlakyTestDetector(DetectionConfig? config = null)
    {
        _config = config ?? new DetectionConfig();
    }

    /// <summary>
    /// Detects flaky tests in the given test assembly
    /// </summary>
    public List<FlakyTestReport> DetectFlakyTests(Assembly testAssembly)
    {
        var testMethods = DiscoverTestMethods(testAssembly);
        var reports = new List<FlakyTestReport>();

        foreach (var testMethod in testMethods)
        {
            var report = AnalyzeTestMethod(testMethod);
            if (report.FlakinessFactor >= _config.MinimumFlakinessThreshold)
            {
                reports.Add(report);
            }
        }

        return reports;
    }

    /// <summary>
    /// Detects flaky tests in a specific test class
    /// </summary>
    public List<FlakyTestReport> DetectFlakyTests(Type testClass)
    {
        var testMethods = DiscoverTestMethods(testClass);
        var reports = new List<FlakyTestReport>();

        foreach (var testMethod in testMethods)
        {
            var report = AnalyzeTestMethod(testMethod);
            if (report.FlakinessFactor >= _config.MinimumFlakinessThreshold)
            {
                reports.Add(report);
            }
        }

        return reports;
    }

    private List<MethodInfo> DiscoverTestMethods(Assembly assembly)
    {
        var testMethods = new List<MethodInfo>();

        foreach (var type in assembly.GetTypes())
        {
            if (type.GetCustomAttribute<TestClassAttribute>() != null)
            {
                testMethods.AddRange(DiscoverTestMethods(type));
            }
        }

        return testMethods;
    }

    private List<MethodInfo> DiscoverTestMethods(Type testClass)
    {
        return testClass.GetMethods()
            .Where(m => m.GetCustomAttribute<TestMethodAttribute>() != null)
            .ToList();
    }

    private FlakyTestReport AnalyzeTestMethod(MethodInfo testMethod)
    {
        var report = new FlakyTestReport
        {
            TestName = $"{testMethod.DeclaringType?.Name}.{testMethod.Name}"
        };

        var results = new List<Models.TestResult>();

        // Sequential execution with different orders
        for (int i = 0; i < _config.RepetitionCount; i++)
        {
            var result = ExecuteTest(testMethod, i);
            results.Add(result);
        }

        // Parallel execution if enabled
        if (_config.EnableParallelExecution)
        {
            var parallelResults = new List<Models.TestResult>();
            Parallel.For(0, _config.RepetitionCount, new ParallelOptions 
            { 
                MaxDegreeOfParallelism = _config.MaxDegreeOfParallelism 
            }, i =>
            {
                var result = ExecuteTest(testMethod, i + _config.RepetitionCount);
                lock (parallelResults)
                {
                    parallelResults.Add(result);
                }
            });
            results.AddRange(parallelResults);
        }

        report.ExecutionHistory = results;
        report.TotalExecutions = results.Count;
        report.PassedExecutions = results.Count(r => r.Passed);
        report.FailedExecutions = results.Count(r => !r.Passed);

        // Calculate flakiness factor
        if (report.PassedExecutions > 0 && report.FailedExecutions > 0)
        {
            // Test is flaky - it sometimes passes and sometimes fails
            report.FlakinessFactor = Math.Min(report.PassedExecutions, report.FailedExecutions) / 
                                     (double)report.TotalExecutions;
        }
        else
        {
            // Test is consistent (always passes or always fails)
            report.FlakinessFactor = 0.0;
        }

        return report;
    }

    private Models.TestResult ExecuteTest(MethodInfo testMethod, int executionOrder)
    {
        var result = new Models.TestResult
        {
            TestName = testMethod.Name,
            ExecutionTime = DateTime.UtcNow,
            ExecutionOrder = executionOrder
        };

        var testClassType = testMethod.DeclaringType;
        if (testClassType == null)
        {
            result.Passed = false;
            result.ErrorMessage = "Test class type is null";
            return result;
        }

        object? testInstance = null;

        try
        {
            var startTime = DateTime.UtcNow;

            // Create test instance
            testInstance = Activator.CreateInstance(testClassType);

            // Run TestInitialize methods
            var initMethods = testClassType.GetMethods()
                .Where(m => m.GetCustomAttribute<TestInitializeAttribute>() != null);
            foreach (var initMethod in initMethods)
            {
                initMethod.Invoke(testInstance, null);
            }

            // Run the test method
            testMethod.Invoke(testInstance, null);

            result.Duration = DateTime.UtcNow - startTime;
            result.Passed = true;
        }
        catch (TargetInvocationException ex)
        {
            var innerEx = ex.InnerException ?? ex;
            result.Passed = false;
            result.ErrorMessage = innerEx.Message;
            result.Duration = TimeSpan.Zero;
        }
        catch (Exception ex)
        {
            result.Passed = false;
            result.ErrorMessage = ex.Message;
            result.Duration = TimeSpan.Zero;
        }
        finally
        {
            // Run TestCleanup methods
            if (testInstance != null)
            {
                try
                {
                    var cleanupMethods = testClassType.GetMethods()
                        .Where(m => m.GetCustomAttribute<TestCleanupAttribute>() != null);
                    foreach (var cleanupMethod in cleanupMethods)
                    {
                        cleanupMethod.Invoke(testInstance, null);
                    }
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }

        return result;
    }
}
