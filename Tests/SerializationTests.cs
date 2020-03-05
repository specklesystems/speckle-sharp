using System;
using Xunit;
using Speckle.Serialisation;
using Speckle.Models;
using System.Collections.Generic;
using Speckle.Kits;
using System.Linq;
using Speckle.Transports;
using System.Diagnostics;
using Xunit.Abstractions;

namespace Tests
{
  public class Serialization
  {
    private readonly ITestOutputHelper output;

    public Serialization(ITestOutputHelper output)
    {
      this.output = output;
    }

    [Fact]
    public void KitsExist()
    {
      var kits = KitManager.Kits;
      Assert.NotEmpty(kits);

      var types = KitManager.Types;
      Assert.NotEmpty(types);
    }

    [Fact]
    public void SimpleSerialization()
    {
      var serializer = new JsonConverter();

      var table = new DiningTable();
      ((dynamic)table)["@wonkyVariable_Name"] = new TableLegFixture();

      var result = serializer.Serialize(table);

      var test = serializer.Deserialize(result);

      Assert.Equal(test.hash, table.hash);
    }

    [Fact]
    public void DiskTransportSerialization()
    {
      var transport = new DiskTransport();
      var serializer = new JsonConverter();

      var table = new DiningTable();

      var result = serializer.SerializeAndSave(table, transport);

      var test = serializer.DeserializeAndGet(result, transport);

      Assert.Equal(test.hash, table.hash);
    }

    [Fact]
    public void MemoryTransportSerialization()
    {
      var transport = new MemoryTransport();
      var serializer = new JsonConverter();

      var table = new DiningTable();

      var result = serializer.SerializeAndSave(table, transport);

      var test = serializer.DeserializeAndGet(result, transport);

      Assert.Equal(test.hash, table.hash);
    }

  }

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
      Assert.True( diff1 < 200, $"Hashing shouldn't take that long ({diff1} ms) for the test object used.");
      output.WriteLine($"Big obj hash duration: {diff1} ms");

      var pt = new Point() { X = 10, Y = 12, Z = 30 };
      stopWatchStep = stopWatch.ElapsedMilliseconds;
      var h3 = pt.hash;

      var diff2 = stopWatch.ElapsedMilliseconds - stopWatchStep;
      Assert.True( diff2 < 10, $"Hashing shouldn't take that long  ({diff2} ms)for the point object used.");
      output.WriteLine($"Small obj hash duration: {diff2} ms");
    }
  }
}
