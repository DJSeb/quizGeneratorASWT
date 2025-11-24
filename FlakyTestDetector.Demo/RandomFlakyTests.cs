using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FlakyTestDetector.Demo;

/// <summary>
/// Example of a flaky test that uses Random without a seed
/// </summary>
[TestClass]
public class RandomFlakyTests
{
    [TestMethod]
    public void Test_RandomFailure()
    {
        // This test is flaky because it uses Random without a seed
        var random = new Random();
        var value = random.Next(0, 10);
        
        // This will sometimes pass and sometimes fail
        Assert.IsTrue(value < 5, $"Expected value < 5, got {value}");
    }

    [TestMethod]
    public void Test_RandomCollectionOrder()
    {
        // This test is flaky due to random shuffling
        var items = new List<int> { 1, 2, 3, 4, 5 };
        var random = new Random();
        var shuffled = items.OrderBy(x => random.Next()).ToList();
        
        // Assumes a specific order which is non-deterministic
        Assert.AreEqual(1, shuffled[0], "First item should be 1");
    }

    [TestMethod]
    public void Test_WithFixedSeed_Stable()
    {
        // This test is stable because it uses a fixed seed
        var random = new Random(42);
        var value = random.Next(0, 10);
        
        // With fixed seed, this will always produce the same value
        Assert.AreEqual(6, value);
    }
}
