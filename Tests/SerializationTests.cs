using System.Collections.Generic;
using Speckle.Transports;
using NUnit.Framework;
using Speckle.Models;
using Speckle.Core;

namespace Tests
{
  [TestFixture]
  public class Serialization
  {

    [Test]
    public void SimpleSerialization()
    {
      var table = new DiningTable();
      ((dynamic)table)["@strangeVariable_NAme3"] = new TableLegFixture();

      var result = Operations.Serialize(table);
      var test = Operations.Deserialize(result);

      Assert.AreEqual(test.GetId(), table.GetId());

      var polyline = new Polyline();
      for (int i = 0; i < 100; i++)
        polyline.Points.Add(new Point() { X = i * 2, Y = i % 2 });

      var strPoly = Operations.Serialize(polyline);
      var dePoly = Operations.Deserialize(strPoly);

      Assert.AreEqual(polyline.GetId(), dePoly.GetId());
    }

    [Test]
    public void ListSerialisation()
    {
      var objs = new List<Base>();
      for (int i = 0; i < 10; i++)
        objs.Add(new Point(i, i, i));

      var result = Operations.Serialize(objs);
      var test = Operations.DeserializeArray(result);
      Assert.AreEqual(10, test.Count);
    }

    //[Test]
    //public void DynamicDispatchment()
    //{
    //  var pt = new Point(1, 2, 3);
    //  ((dynamic)pt).HelloWorld = "whatever";
    //  ((dynamic)pt)["@detach_me"] = new Point(3, 4, 5);
    //  ((dynamic)pt)["@detach_me_too"] = new Point(3, 4, 5); // same point, same hash, should not create a new object in the transport.

    //  var transport = new MemoryTransport();
    //  var serializer = new Serializer();

    //  var result = serializer.SerializeAndSave(pt, transport);

    //  Assert.AreEqual(2, transport.Objects.Count);

    //  var deserialized = serializer.DeserializeAndGet(result, transport);

    //  Assert.AreEqual(pt.hash, deserialized.hash);
    //  Assert.AreEqual(((dynamic)pt).HelloWorld, "whatever");
    //}

    [Test]
    public void SerialisationAbstractObjects()
    {
      var nk = new NonKitClass() { TestProp = "Hello", Numbers = new List<int>() { 1, 2, 3, 4, 5 } };
      var abs = new Abstract(nk);

      var transport = new MemoryTransport();

      var abs_serialized = Operations.Serialize(abs);
      var abs_deserialized = Operations.Deserialize(abs_serialized);
      var abs_se_deserializes = Operations.Serialize(abs_deserialized);

      Assert.AreEqual(abs.GetId(), abs_deserialized.GetId());
      Assert.AreEqual(abs.@base.GetType(), ((Abstract)abs_deserialized).@base.GetType());
    }

    [Test]
    public void IgnoreCircularReferences()
    {
      var pt = new Point(1, 2, 3);
      ((dynamic)pt).circle = pt;

      var test = Operations.Serialize(pt);

      var result = Operations.Deserialize(test);
      var circle = ((dynamic)result).circle;

      Assert.Null(circle);
    }

  }
}
