using System.Collections.Generic;
using Speckle.Core.Transports;
using NUnit.Framework;
using Speckle.Core.Models;
using Speckle.Core.Api;

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
    public void ListSerialisationAndDeserialisation()
    {
      var objs = new List<Base>();
      for (int i = 0; i < 10; i++)
        objs.Add(new Point(i, i, i));

      var result = Operations.Serialize(objs);
      var test = Operations.DeserializeArray(result);
      Assert.AreEqual(10, test.Count);
    }

    [Test]
    public void DictionarySerialisation()
    {
      // TODO
      var dict = new Dictionary<string, Base>();
      for (int i = 0; i < 10; i++)
        dict[$"key{i}"] = new Point(i, i, i);

      var result = Operations.Serialize(dict);
      var test = Operations.DeserializeDictionary(result);

      Assert.AreEqual(test.Keys, dict.Keys);
    }

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

    [Test]
    public void InterfacePropHandling()
    {
      var cat = new PolygonalFeline();

      cat.Tail = new Line()
      {
        Start = new Point(0, 0, 0),
        End = new Point(42, 42, 42)
      };

      for (int i = 0; i < 10; i++)
      {
        cat.Claws[$"Claw number {i}"] = new Line { Start = new Point(i, i, i), End = new Point(i + 3.14, i + 3.14, i + 3.14) };

        if (i % 2 == 0)
          cat.Whiskers.Add(new Line { Start = new Point(i / 2, i / 2, i / 2), End = new Point(i + 3.14, i + 3.14, i + 3.14) });
        else
        {
          var brokenWhisker = new Polyline();
          brokenWhisker.Points.Add(new Point(-i, 0, 0));
          brokenWhisker.Points.Add(new Point(0, 0, 0));
          brokenWhisker.Points.Add(new Point(i, 0, 0));
          cat.Whiskers.Add(brokenWhisker);
        }

        cat.Fur[i] = new Line { Start = new Point(i, i, i), End = new Point(i + 3.14, i + 3.14, i + 3.14) };
      }

      var result = Operations.Serialize(cat);

      var deserialisedFeline = Operations.Deserialize(result);

      Assert.AreEqual(cat.GetId(), deserialisedFeline.GetId()); // If we're getting the same hash... we're probably fine!
    }

    [Test]
    public void InheritedTests()
    {
      var superPoint = new SuperPoint() { X = 10, Y = 10, Z = 10, W = 42 };

      var str = Operations.Serialize(superPoint);
      var sstr = str;
    }


  }
}
