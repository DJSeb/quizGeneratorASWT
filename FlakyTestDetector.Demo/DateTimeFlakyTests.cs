using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FlakyTestDetector.Demo;

/// <summary>
/// Example of a flaky test that depends on DateTime.Now
/// </summary>
[TestClass]
public class DateTimeFlakyTests
{
    [TestMethod]
    public void Test_FailsOnWeekend()
    {
        // This test is flaky because it depends on the current date
        var today = DateTime.Now;
        Assert.IsTrue(today.DayOfWeek != DayOfWeek.Saturday && today.DayOfWeek != DayOfWeek.Sunday,
            "Test cannot run on weekends!");
    }

    [TestMethod]
    public void Test_FailsAfterNoon()
    {
        // This test is flaky because it depends on the time of day
        var now = DateTime.Now;
        Assert.IsTrue(now.Hour < 12, "Test must run before noon!");
    }

    [TestMethod]
    public void Test_OnlyPassesInCurrentMonth()
    {
        // This test depends on the current month
        var now = DateTime.Now;
        Assert.AreEqual(11, now.Month, "Test only works in November!");
    }
}
