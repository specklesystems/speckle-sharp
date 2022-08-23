using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Speckle.Core.Api;
using Speckle.Core.Models;
using Speckle.Core.Models.Extensions;

namespace Tests
{
  [TestFixture]
  public class TraversalTests
  {

    private static List<(Base rootObject, int expectedCount)> TestData = new List<(Base rootObject, int expectedCount)>
    {
      //(Helpers.Receive("https://latest.speckle.dev/streams/24c3741255/commits/0edde983dc").Result, 2673), //Rhino //TODO this commit seems to traverse differently on Circle CI (by 1 object) perhaps due to some non-deterministic deserialisation?
      (Helpers.Receive("https://latest.speckle.dev/streams/24c3741255/commits/1fe2536d62").Result, 1636), //Revit Gergo project
    };
    
    
    [Test, TestCaseSource(nameof(TestData))]
    public void TestExpectedCount((Base rootObject, int expectedCount) t)
    {
      var ret = t.rootObject.Flatten();

      Assert.AreEqual(t.expectedCount, ret.Count);
      Assert.That(ret, Is.Unique);
    }

        
    [Test, TestCaseSource(nameof(TestData))]
    public void TestBreakerFixed((Base rootObject, int) t)
    {
      //Test breaking after fixed number of items
      int counter = 0;
      var ret = t.rootObject.Flatten(b => ++counter >= 5);

      Assert.AreEqual(5, ret.Count());
      Assert.That(ret, Is.Unique);
    }
    
  }
  
}
