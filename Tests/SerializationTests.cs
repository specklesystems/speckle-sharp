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
  public class Serialization
  {
    private readonly ITestOutputHelper output;

    public Serialization(ITestOutputHelper output)
    {
      this.output = output;
    }

    [Fact]
    public void SimpleSerialization()
    {
      var serializer = new Serializer();

      var table = new DiningTable();
      ((dynamic)table)["@wonkyVariable_Name"] = new TableLegFixture();

      var result = serializer.Serialize(table);

      var test = serializer.Deserialize(result);

      Assert.Equal(test.hash, table.hash);

      var polyline = new Polyline();

      for (int i = 0; i < 100; i++)
        polyline.Points.Add(new Point() { X = i * 2, Y = i % 2 });

      var strPoly = serializer.Serialize(polyline);
      var dePoly = serializer.Deserialize(strPoly);

      Assert.Equal(polyline.hash, dePoly.hash);

    }

    [Fact]
    public void DiskTransportSerialization()
    {
      var transport = new DiskTransport();
      var serializer = new Serializer();

      var table = new DiningTable();

      var result = serializer.SerializeAndSave(table, transport);

      var test = serializer.DeserializeAndGet(result, transport);

      Assert.Equal(test.hash, table.hash);
    }

    [Fact]
    public void MemoryTransportSerialization()
    {
      var transport = new MemoryTransport();
      var serializer = new Serializer();

      var table = new DiningTable();

      var result = serializer.SerializeAndSave(table, transport);

      var test = serializer.DeserializeAndGet(result, transport);

      Assert.Equal(test.hash, table.hash);
    }

    [Fact]
    public void DynamicDispatchment()
    {
      var pt = new Point(1, 2, 3);
      ((dynamic)pt)["@detach_me"] = new Point(3, 4, 5);
      ((dynamic)pt)["@detach_me_too"] = new Point(3, 4, 5); // same point, same hash, should not create a new object in the transport.

      var transport = new MemoryTransport();
      var serializer = new Serializer();

      var result = serializer.SerializeAndSave(pt, transport);

      Assert.Equal(2, transport.Objects.Count);

      var deserialized = serializer.DeserializeAndGet(result, transport);

      Assert.Equal(pt.hash, deserialized.hash);

    }

    [Fact]
    public void AbstractObjectHandling()
    {
      var nk = new NonKitClass() { TestProp = "Hello", Numbers = new List<int>() { 1, 2, 3, 4, 5 } };
      var abs = new Abstract(nk);

      var transport = new MemoryTransport();
      var serializer = new Serializer();

      var abs_serialized = serializer.Serialize(abs);
      var abs_deserialized = serializer.Deserialize(abs_serialized);
      var abs_se_deserializes = serializer.Serialize(abs_deserialized);

      Assert.Equal(abs.hash, abs_deserialized.hash);
      Assert.Equal(abs.@base.GetType(), ((Abstract)abs_deserialized).@base.GetType());
    }

    [Fact]
    public void IgnoreCircularReferences()
    {
      var pt = new Point(1,2,3);
      ((dynamic)pt).circle = pt;

      var test = (new Serializer()).Serialize(pt);
      var tt = test;

      var memTransport = new MemoryTransport();
      var test2 = (new Serializer()).SerializeAndSave(pt, memTransport);
      var ttt = test2;

      var test2_deserialized = (new Serializer()).DeserializeAndGet(ttt, memTransport);
      var t = test2_deserialized;
    }

  }
}
