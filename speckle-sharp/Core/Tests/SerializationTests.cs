using System.Collections.Generic;
using NUnit.Framework;
using Speckle.Core.Api;
using Speckle.Core.Models;
using Speckle.Core.Transports;

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
      {
        polyline.Points.Add(new Point() { X = i * 2, Y = i % 2 });
      }

      var strPoly = Operations.Serialize(polyline);
      var dePoly = Operations.Deserialize(strPoly);

      Assert.AreEqual(polyline.GetId(), dePoly.GetId());
    }

    [Test]
    public void DictionarySerialisation()
    {
      // TODO
      var dict = new Dictionary<string, Base>();
      for (int i = 0; i < 10; i++)
      {
        dict[$"key{i}"] = new Point(i, i, i);
      }

      var result = Operations.Serialize(dict);
      var test = Operations.DeserializeDictionary(result);

      Assert.AreEqual(test.Keys, dict.Keys);
    }

    [Test]
    public void IgnoreCircularReferences()
    {
      var pt = new Point(1, 2, 3);
      pt["circle"] = pt;

      var test = Operations.Serialize(pt);

      var result = Operations.Deserialize(test);
      var circle = result["circle"];
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
        {
          cat.Whiskers.Add(new Line { Start = new Point(i / 2, i / 2, i / 2), End = new Point(i + 3.14, i + 3.14, i + 3.14) });
        }
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
    public void InheritanceTests()
    {
      var superPoint = new SuperPoint() { X = 10, Y = 10, Z = 10, W = 42 };

      var str = Operations.Serialize(superPoint);
      var sstr = Operations.Deserialize(str);

      Assert.AreEqual(superPoint.speckle_type, sstr.speckle_type);
    }

    [Test]
    public void ListDynamicProp()
    {
      var point = new Point();
      var test = new List<Base>();

      for (var i = 0; i < 100; i++)
      {
        test.Add(new SuperPoint { W = i });
      }

      point["test"] = test;

      var str = Operations.Serialize(point);
      var dsrls = Operations.Deserialize(str);

      var list = dsrls["test"] as List<object>; // NOTE: on dynamically added lists, we cannot infer the inner type and we always fall back to a generic list<object>.
      Assert.AreEqual(100, list.Count);
    }

    [Test]
    public void ChunkSerialisation()
    {
      var baseBasedChunk = new DataChunk();
      for (var i = 0; i < 200; i++)
      {
        baseBasedChunk.data.Add(new SuperPoint { W = i });
      }

      var stringBasedChunk = new DataChunk();
      for (var i = 0; i < 200; i++)
      {
        stringBasedChunk.data.Add(i + "_hai");
      }

      var doubleBasedChunk = new DataChunk();
      for (var i = 0; i < 200; i++)
      {
        doubleBasedChunk.data.Add(i + 0.33);
      }

      var baseChunkString = Operations.Serialize(baseBasedChunk);
      var stringChunkString = Operations.Serialize(stringBasedChunk);
      var doubleChunkString = Operations.Serialize(doubleBasedChunk);

      var baseChunkDeserialised = (DataChunk) Operations.Deserialize(baseChunkString);
      var stringChunkDeserialised = (DataChunk) Operations.Deserialize(stringChunkString);
      var doubleChunkDeserialised = (DataChunk) Operations.Deserialize(doubleChunkString);

      Assert.AreEqual(baseBasedChunk.data.Count, baseChunkDeserialised.data.Count);
      Assert.AreEqual(stringBasedChunk.data.Count, stringChunkDeserialised.data.Count);
      Assert.AreEqual(doubleBasedChunk.data.Count, doubleChunkDeserialised.data.Count);
    }

    [Test]
    public void ObjectWithChunksSerialisation()
    {
      int MAX_NUM = 2020;
      var mesh = new FakeMesh();

      mesh.ArrayOfDoubles = new double[MAX_NUM];
      mesh.ArrayOfLegs = new TableLeg[MAX_NUM];
      var customChunk = new List<double>();
      var defaultChunk = new List<double>();

      for (int i = 0; i < MAX_NUM; i++)
      {
        mesh.Vertices.Add(i / 2);
        customChunk.Add(i / 2);
        defaultChunk.Add(i / 2);
        mesh.Tables.Add(new Tabletop { length = 2000 });
        mesh.ArrayOfDoubles[i] = i * 3.3;
        mesh.ArrayOfLegs[i] = new TableLeg { height = 2 + i };
      }
      
      mesh["@(800)CustomChunk"] = customChunk;
      mesh["@()DefaultChunk"] = defaultChunk;

      var serialised = Operations.Serialize(mesh);
      var deserialised = Operations.Deserialize(serialised);

      Assert.AreEqual(deserialised.GetId(), mesh.GetId());
    }

    [Test]
    public void EmptyListSerialisationTests()
    {
      // NOTE: expected behaviour is that empty lists should serialize as empty lists. Don't ask why, it's complicated. 
      // Regarding chunkable empty lists, to prevent empty chunks, the expected behaviour is to have an empty lists, with no chunks inside.
      var test = new Base();

      test["@(5)emptyChunks"] = new List<object>();
      test["emptyList"] = new List<object>();
      test["@emptyDetachableList"] = new List<object>();

      // Note: nested empty lists should be preserved. 
      test["nestedList"] = new List<object>() { new List<object>() { new List<object>() } };
      test["@nestedDetachableList"] = new List<object>() { new List<object>() { new List<object>() } };
      
      var serialised = Operations.Serialize(test);
      var isCorrect =
        serialised.Contains("\"@(5)emptyChunks\":[]") &&
        serialised.Contains("\"emptyList\":[]") &&
        serialised.Contains("\"@emptyDetachableList\":[]") &&
        serialised.Contains("\"nestedList\":[[[]]]") &&
        serialised.Contains("\"@nestedDetachableList\":[[[]]]");

      Assert.AreEqual(isCorrect, true);
    }

  }
}
