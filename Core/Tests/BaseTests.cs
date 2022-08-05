using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Tests;

namespace Tests
{
  [TestFixture]
  public class BaseTests
  {
    [Test]
    public void CanGetSetDynamicItemProp()
    {
      var @base = new Base();
      @base["Item"] = "Item";

      Assert.AreEqual(@base["Item"], "Item");
    }

    [Test]
    public void CanGetSetTypedItemProp()
    {
      var @base = new ObjectWithItemProp();
      @base.Item = "baz";

      Assert.AreEqual(@base["Item"], "baz");
      Assert.AreEqual(@base.Item, "baz");
    }

    [Test(Description = "Checks if validation is performed in property names")]
    public void CanValidatePropNames()
    {
      dynamic @base = new Base();

      // Word chars are OK
      @base["something"] = "B";

      // Only single leading @ allowed
      @base["@something"] = "A";
      Assert.Throws<SpeckleException>(() => { @base["@@@something"] = "Testing"; });

      // Invalid chars:  ./
      Assert.Throws<SpeckleException>(() => { @base["some.thing"] = "Testing"; });
      Assert.Throws<SpeckleException>(() => { @base["some/thing"] = "Testing"; });

      // Trying to change a class member value will throw exceptions.
      //Assert.Throws<Exception>(() => { @base["speckle_type"] = "Testing"; });
      //Assert.Throws<Exception>(() => { @base["id"] = "Testing"; });
    }

    [Test]
    public void CountDynamicChunkables()
    {
      int MAX_NUM = 3000;
      var @base = new Base();
      var customChunk = new List<double>();
      var customChunkArr = new double[MAX_NUM];

      for (int i = 0; i < MAX_NUM; i++)
      {
        customChunk.Add(i / 2);
        customChunkArr[i] = i;
      }

      @base["@(1000)cc1"] = customChunk;
      @base["@(1000)cc2"] = customChunkArr;

      var num = @base.GetTotalChildrenCount();
      Assert.AreEqual(MAX_NUM / 1000 * 2 + 1, num);
    }

    [Test]
    public void CountTypedChunkables()
    {
      int MAX_NUM = 3000;
      var @base = new SampleObject();
      var customChunk = new List<double>();
      var customChunkArr = new double[MAX_NUM];

      for (int i = 0; i < MAX_NUM; i++)
      {
        customChunk.Add(i / 2);
        customChunkArr[i] = i;
      }

      @base.list = customChunk;
      @base.arr = customChunkArr;

      var num = @base.GetTotalChildrenCount();
      var actualNum = 1 + MAX_NUM / 300 + MAX_NUM / 1000;
      Assert.AreEqual(actualNum, num);
    }

    [Test(Description = "Checks that no ignored or obsolete properties are returned")]
    public void CanGetMemberNames()
    {
      var @base = new SampleObject();
      @base["dynamicProp"] = 123;
      var names = @base.GetMemberNames();
      var propName = "IgnoredSchemaProp";
      Assert.False(names.Contains(propName));
      Assert.False(names.Contains("DeprecatedSchemaProp"));
    }

    [Test(Description = "Checks that no ignored or obsolete properties are returned")]
    public void CanGetMembers()
    {
      var @base = new SampleObject();
      @base["dynamicProp"] = 123;

      var names = @base.GetMembers().Keys;
      Assert.False(names.Contains("IgnoredSchemaProp"));
      Assert.False(names.Contains("DeprecatedSchemaProp"));
    }
    
    [Test(Description = "Checks that no ignored or obsolete properties are returned")]
    public void CanGetDynamicMembers()
    {
      var @base = new SampleObject();
      @base["dynamicProp"] = null;

      var names = @base.GetDynamicMemberNames();
      Assert.True(names.Contains("dynamicProp"));
      Assert.True(@base["dynamicProp"] == null);
    }

    [Test]
    public void CanSetDynamicMembers()
    {
      var @base = new SampleObject();
      var key = "dynamicProp";
      var value = "something";
      // Can create a new dynamic member
      @base[key] = value;
      Assert.True((string)@base[key] == value);
      
      // Can overwrite existing
      value = "some other value";
      @base[key] = value;
      Assert.True((string)@base[key] == value);
      
      // Accepts null values
      @base[key] = null;
      Assert.True(@base[key] == null);
    }
    
    
    public class SampleObject : Base
    {
      [Chunkable]
      [DetachProperty]
      public List<double> list { get; set; } = new List<double>();

      [Chunkable(300)]
      [DetachProperty]
      public double[] arr { get; set; }

      [DetachProperty]
      public SampleProp detachedProp { get; set; }

      public SampleProp attachedProp { get; set; }

      public string @crazyProp { get; set; }

      [SchemaIgnore]
      public string IgnoredSchemaProp { get; set; }

      [Obsolete("Use attached prop")]
      public string ObsoleteSchemaProp { get; set; }
      
      public SampleObject() { }
    }

    public class SampleProp
    {
      public string name { get; set; }
    }

    public class ObjectWithItemProp : Base
    {
      public string Item { get; set; } = "Item";
    }
  }
}
