using Xunit;
using Speckle.Serialisation;
using System.Collections.Generic;
using Speckle.Kits;
using Speckle.Core;
using Speckle.Transports;
using System.Diagnostics;
using Xunit.Abstractions;

namespace Tests
{
  public class Hashing
  {

    private readonly ITestOutputHelper output;

    public Hashing(ITestOutputHelper output)
    {
      this.output = output;
    }

    [Fact]
    public void HashChangeCheck()
    {
      var table = new DiningTable();
      var secondTable = new DiningTable();

      Assert.Equal(table.hash, secondTable.hash);

      ((dynamic)secondTable).testProp = "wonderful";

      Assert.NotEqual(table.hash, secondTable.hash);
    }

    [Fact]
    public void IgnoredDynamicPropertiesCheck()
    {
      var table = new DiningTable();
      var originalHash = table.hash;

      ((dynamic)table).__testProp = "wonderful";

      Assert.Equal(originalHash, table.hash);
    }

    [Fact]
    public void IgnoreFlaggedProperties()
    {
      var table = new DiningTable();
      var h1 = table.hash;
      table.HashIngoredProp = "adsfghjkl";

      Assert.Equal(h1, table.hash);
    }

    [Fact]
    public void HashingPerformance()
    {
      var polyline = new Polyline();

      for (int i = 0; i < 10000; i++)
        polyline.Points.Add(new Point() { X = i * 2, Y = i % 2 });

      var stopWatch = new Stopwatch();
      stopWatch.Start();

      // Warm-up: first hashing always takes longer due to json serialisation init
      var h1 = polyline.hash;
      var stopWatchStep = stopWatch.ElapsedMilliseconds;

      stopWatchStep = stopWatch.ElapsedMilliseconds;
      var h2 = polyline.hash;

      var diff1 = stopWatch.ElapsedMilliseconds - stopWatchStep;
      Assert.True(diff1 < 200, $"Hashing shouldn't take that long ({diff1} ms) for the test object used.");
      output.WriteLine($"Big obj hash duration: {diff1} ms");

      var pt = new Point() { X = 10, Y = 12, Z = 30 };
      stopWatchStep = stopWatch.ElapsedMilliseconds;
      var h3 = pt.hash;

      var diff2 = stopWatch.ElapsedMilliseconds - stopWatchStep;
      Assert.True(diff2 < 10, $"Hashing shouldn't take that long  ({diff2} ms)for the point object used.");
      output.WriteLine($"Small obj hash duration: {diff2} ms");
    }

    [Fact]
    public void AbstractHashing()
    {
      var nk1 = new NonKitClass();
      var abs1 = new Abstract(nk1);

      var nk2 = new NonKitClass() { TestProp = "HEllo" };
      var abs2 = new Abstract(nk2);

      var abs1H = abs1.hash;
      var abs2H = abs2.hash;

      Assert.NotEqual(abs1H, abs2H);

      nk1.TestProp = "Wow";

      Assert.NotEqual(abs1H, abs1.hash);
    }
  }
}
