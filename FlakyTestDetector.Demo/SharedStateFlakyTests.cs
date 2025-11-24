using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FlakyTestDetector.Demo;

/// <summary>
/// Example of tests with shared mutable state causing order dependencies
/// </summary>
[TestClass]
public class SharedStateFlakyTests
{
    private static int _sharedCounter = 0;
    private static List<string> _sharedList = new();

    [TestMethod]
    public void Test_IncrementCounter()
    {
        // This test modifies shared state
        _sharedCounter++;
        Assert.AreEqual(1, _sharedCounter, "Counter should be 1");
    }

    [TestMethod]
    public void Test_CounterIsZero()
    {
        // This test assumes counter is 0, but depends on test execution order
        Assert.AreEqual(0, _sharedCounter, "Counter should start at 0");
    }

    [TestMethod]
    public void Test_AddToSharedList()
    {
        _sharedList.Add("test");
        Assert.AreEqual(1, _sharedList.Count);
    }

    [TestMethod]
    public void Test_SharedListIsEmpty()
    {
        // This test assumes list is empty, but depends on test execution order
        Assert.AreEqual(0, _sharedList.Count, "List should be empty");
    }

    [TestCleanup]
    public void Cleanup()
    {
        // Even with cleanup, static state persists across tests in the same run
        // This is a common source of flakiness
    }
}
