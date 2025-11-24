using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FlakyTestDetector.Demo;

/// <summary>
/// Example of tests with timing and concurrency issues
/// </summary>
[TestClass]
public class ConcurrencyFlakyTests
{
    private int _counter = 0;

    [TestMethod]
    public void Test_ConcurrentIncrement()
    {
        // This test has a race condition
        var tasks = new List<Task>();
        
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                for (int j = 0; j < 100; j++)
                {
                    _counter++; // Race condition!
                }
            }));
        }

        Task.WaitAll(tasks.ToArray());
        
        // Expected 1000, but due to race conditions, might be less
        Assert.AreEqual(1000, _counter, $"Expected 1000 but got {_counter}");
    }

    [TestMethod]
    public async Task Test_TimingDependency()
    {
        // This test depends on precise timing
        var startTime = DateTime.UtcNow;
        
        await Task.Delay(100); // Simulate some work
        
        var elapsed = DateTime.UtcNow - startTime;
        
        // This is flaky because task scheduling is non-deterministic
        Assert.IsTrue(elapsed.TotalMilliseconds >= 100 && elapsed.TotalMilliseconds < 150,
            $"Expected 100-150ms, got {elapsed.TotalMilliseconds}ms");
    }

    [TestMethod]
    public void Test_ThreadSleepFlaky()
    {
        var startTime = DateTime.UtcNow;
        Thread.Sleep(50);
        var elapsed = DateTime.UtcNow - startTime;
        
        // Flaky due to OS scheduling
        Assert.IsTrue(elapsed.TotalMilliseconds < 100, 
            $"Sleep should be fast, took {elapsed.TotalMilliseconds}ms");
    }
}
