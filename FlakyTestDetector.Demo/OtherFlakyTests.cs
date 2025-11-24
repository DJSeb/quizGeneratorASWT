using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FlakyTestDetector.Demo;

/// <summary>
/// Example of tests with GUID and file system issues
/// </summary>
[TestClass]
public class OtherFlakyTests
{
    [TestMethod]
    public void Test_GuidGeneration()
    {
        // GUIDs are non-deterministic
        var guid1 = Guid.NewGuid();
        var guid2 = Guid.NewGuid();
        
        // This test is contrived but shows GUID-related flakiness
        Assert.IsTrue(guid1.ToString()[0] < 'f', 
            $"GUID should start with low hex digit, got {guid1}");
    }

    [TestMethod]
    public void Test_FileSystemDependency()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), "test-file.txt");
        
        // May fail if file already exists from previous run
        File.WriteAllText(tempFile, "test content");
        
        Assert.IsTrue(File.Exists(tempFile));
        
        // Should clean up but might not if test fails
        File.Delete(tempFile);
    }

    [TestMethod]
    public void Test_UnsortedCollectionOrder()
    {
        // Dictionary/HashSet order is not guaranteed
        var dict = new Dictionary<string, int>
        {
            { "a", 1 },
            { "b", 2 },
            { "c", 3 }
        };
        
        var keys = dict.Keys.ToList();
        
        // This assumes a specific order which is not guaranteed
        Assert.AreEqual("a", keys[0], "First key should be 'a'");
    }

    [TestMethod]
    public void Test_StableWithDeterministicGuid()
    {
        // Using a deterministic GUID is stable
        var guid = new Guid("12345678-1234-1234-1234-123456789012");
        Assert.AreEqual("12345678-1234-1234-1234-123456789012", guid.ToString());
    }
}
