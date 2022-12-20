using Newtonsoft.Json;
using NUnit.Framework;
using Speckle.Core.Api;
using Speckle.Core.Models;
using Speckle.Core.Transports;

namespace Tests
{
  [TestFixture]
  public class Closures
  {

    [Test(Description = "Checks wether closures are generated correctly by the serialiser.")]
    public void CorrectDecompositionTracking()
    {
      var d5 = new Base();
      ((dynamic)d5).name = "depth five"; // end v

      var d4 = new Base();
      ((dynamic)d4).name = "depth four";
      ((dynamic)d4)["@detach"] = d5;

      var d3 = new Base();
      ((dynamic)d3).name = "depth three";
      ((dynamic)d3)["@detach"] = d4;

      var d2 = new Base();
      ((dynamic)d2).name = "depth two";
      ((dynamic)d2)["@detach"] = d3;
      ((dynamic)d2)["@joker"] = new object[] { d5 };

      var d1 = new Base();
      ((dynamic)d1).name = "depth one";
      ((dynamic)d1)["@detach"] = d2;
      ((dynamic)d1)["@joker"] = d5; // consequently, d5 depth in d1 should be 1

      var transport = new MemoryTransport();

      var result = Operations.Send(d1, new List<ITransport>() { transport }, false).Result;

      var test = Operations.Receive(result, localTransport: transport).Result;

      Assert.IsNotNull(test.id);
      Assert.That(d1.GetId(true), Is.EqualTo(test.id));

      var d1_ = JsonConvert.DeserializeObject<dynamic>(transport.Objects[d1.GetId(true)]);
      var d2_ = JsonConvert.DeserializeObject<dynamic>(transport.Objects[d2.GetId(true)]);
      var d3_ = JsonConvert.DeserializeObject<dynamic>(transport.Objects[d3.GetId(true)]);
      var d4_ = JsonConvert.DeserializeObject<dynamic>(transport.Objects[d4.GetId(true)]);
      var d5_ = JsonConvert.DeserializeObject<dynamic>(transport.Objects[d5.GetId(true)]);


      var depthOf_d5_in_d1 = int.Parse((string)d1_.__closure[d5.GetId(true)]);
      Assert.That(depthOf_d5_in_d1, Is.EqualTo(1));

      var depthOf_d4_in_d1 = int.Parse((string)d1_.__closure[d4.GetId(true)]);
      Assert.That(depthOf_d4_in_d1, Is.EqualTo(3));

      var depthOf_d5_in_d3 = int.Parse((string)d3_.__closure[d5.GetId(true)]);
      Assert.That(depthOf_d5_in_d3, Is.EqualTo(2));

      var depthOf_d4_in_d3 = int.Parse((string)d3_.__closure[d4.GetId(true)]);
      Assert.That(depthOf_d4_in_d3, Is.EqualTo(1));

      var depthOf_d5_in_d2 = int.Parse((string)d2_.__closure[d5.GetId(true)]);
      Assert.That(depthOf_d5_in_d2, Is.EqualTo(1));
    }

    [Test]
    public void DescendantsCounting()
    {
      Base myBase = new Base();

      var myList = new List<object>();
      // These should be counted! 
      for (int i = 0; i < 100; i++)
      {
        var smolBase = new Base();
        smolBase["test"] = i;
        myList.Add(smolBase);
      }

      // Primitives should not be counted! 
      for (int i = 0; i < 10; i++)
      {
        myList.Add(i);
      }
      myList.Add("Hello");
      myList.Add(new { hai = "bai" });

      myBase["@detachTheList"] = myList;

      var dictionary = new Dictionary<string, object>();
      for (int i = 0; i < 10; i++)
      {
        var smolBase = new Base() { applicationId = i.ToString() };
        dictionary[$"key {i}"] = smolBase;
      }

      dictionary["string value"] = "bol";
      dictionary["int value"] = 42;
      dictionary["THIS IS RECURSIVE SURPRISE"] = myBase;

      myBase["@detachTheDictionary"] = dictionary;

      var count = myBase.GetTotalChildrenCount();
      Assert.That(count, Is.EqualTo(112));

      var tableTest = new DiningTable();
      var tableKidsCount = tableTest.GetTotalChildrenCount();
      Assert.That(tableKidsCount, Is.EqualTo(10));

      // Explicitely test for recurisve references! 
      var recursiveRef = new Base() { applicationId = "random" };
      recursiveRef["@recursive"] = recursiveRef;

      var supriseCount = recursiveRef.GetTotalChildrenCount();
      Assert.That(supriseCount, Is.EqualTo(2));
    }

  }
}
