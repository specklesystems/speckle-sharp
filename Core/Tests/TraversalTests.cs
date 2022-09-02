using System;
using System.Linq;
using NUnit.Framework;
using Speckle.Core.Models;
using Speckle.Core.Models.Extensions;

namespace Tests.Models
{
  [TestFixture, TestOf(typeof(BaseExtensions))]
  public class TraversalTests
  {

    [Test] [Description("Tests that provided breaker rules are respected")]
    public void TestFlattenWithBreaker()
    {
      //Setup
      Base root = new Base()
      {
        id = "root",
        ["child"] = new Base()
        {
          id = "traverse through me",
          ["child"] = new Base()
          {
            id = "break on me, go no further",
            ["child"] = new Base()
            {
              id = "should have ignored me"
            }
          }
        }
      };
      
      bool BreakRule(Base b)
      {
        return b.id.Contains("break on me");
      }
      
      //Flatten
      var ret = root.Flatten(BreakRule).ToList();
      
      //Test
      Assert.That(ret, Has.Count.EqualTo(3));
      Assert.That(ret, Is.Unique);
      Assert.That(ret.Where(BreakRule), Is.Not.Empty);
      Assert.That(ret.Where(x => x.id.Contains("should have ignored me")), Is.Empty);
    }
    
    
    [Test]
    [TestCase(5,5)]
    [TestCase(5,10)]
    [TestCase(10,5)]
    [Description("Tests breaking after a fixed number of items")]
    public void TestBreakerFixed(int nestDepth, int flattenDepth)
    {
      //Setup
      Base rootObject = new Base() {id = "0"};
      Base lastNode = rootObject;
      for (int i = 1; i < nestDepth; i++)
      {
        Base newNode = new Base() {id = $"{i}"};
        lastNode["child"] = newNode;
        lastNode = newNode;
      }
      
      //Flatten
      int counter = 0;
      var ret = rootObject.Flatten(b => ++counter >= flattenDepth).ToList();;

      //Test
      Assert.That(ret, Has.Count.EqualTo(Math.Min(flattenDepth, nestDepth)));
      Assert.That(ret, Is.Unique);
    }
    
  
    [Test, Timeout(2000)]
    [Description("Tests that the flatten function does not get stuck on circular references")]
    public void TestCircularReference()
    {
      //Setup 
      Base objectA = new Base() {id = "a"};
      Base objectB = new Base() {id = "b"};
      Base objectC = new Base() {id = "c"};

      objectA["child"] = objectB;
      objectB["child"] = objectC;
      objectC["child"] = objectA;
      
      
      //Flatten
      var ret = objectA.Flatten().ToList();

      //Test
      Assert.That(ret, Is.Unique);
      Assert.That(ret, Is.EquivalentTo(new[]{ objectA, objectB, objectC }));
      Assert.That(ret, Has.Count.EqualTo(3));
    }

    [Test]
    [Description("Tests that the flatten function correctly handles (non circular) duplicates")]
    public void TestDuplicates()
    {
      //Setup 
      Base objectA = new Base() {id = "a"};
      Base objectB = new Base() {id = "b"};

      objectA["child1"] = objectB;
      objectA["child2"] = objectB;
      
      //Flatten
      var ret = objectA.Flatten().ToList();;

      //Test
      Assert.That(ret, Is.Unique);
      Assert.That(ret, Is.EquivalentTo(new[]{ objectA, objectB }));
      Assert.That(ret, Has.Count.EqualTo(2));
    }
  }
  
}
