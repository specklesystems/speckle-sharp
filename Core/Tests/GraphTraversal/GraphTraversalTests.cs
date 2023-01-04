using System.Collections;
using NUnit.Framework;
using Speckle.Core.Models;
using Speckle.Core.Models.GraphTraversal;

namespace TestsUnit.ObjectTraversal;

[TestFixture, TestOf(typeof(GraphTraversal))]
public class GraphTraversalTests
{
  
  private static IEnumerable<TraversalContext> Traverse(Base testCase, params ITraversalRule[] rules)
  {
    var sut = new GraphTraversal(rules);
    return sut.Traverse(testCase);
  }

  [Test]
  public void Traverse_TraversesListMembers()
  {
    var traverseListsRule = TraversalRule.NewTraveralRule()
      .When(_ => true)
      .ContinueTraversing(x => x.GetMembers(DynamicBaseMemberType.All)
        .Where(p => p.Value is IList)
        .Select(kvp => kvp.Key)
      );

    var expectTraverse = new Base() { id = "List Member" };
    var expectIgnored = new Base() { id = "Not List Member" };

    TraversalMock testCase = new TraversalMock()
    {
      ListChildren = new List<Base>() { expectTraverse },
      DictChildren = new Dictionary<string, Base>() { ["myprop"] = expectIgnored },
      Child = expectIgnored,
    };

    var ret = Traverse(testCase, traverseListsRule).Select(b => b.current).ToList();

    //Assert expected members present
    Assert.That(ret, Has.Exactly(1).Items.EqualTo(testCase));
    Assert.That(ret, Has.Exactly(1).Items.EqualTo(expectTraverse));
    
    //Assert unexpected members not present
    Assert.That(ret, Has.No.Member(expectIgnored));
    Assert.That(ret, Has.Count.EqualTo(2));
  }
  
  [Test]
  public void Traverse_TraversesDictMembers()
  {
    var traverseListsRule = TraversalRule.NewTraveralRule()
      .When(_ => true)
      .ContinueTraversing(x => x.GetMembers(DynamicBaseMemberType.All)
        .Where(p => p.Value is IDictionary)
        .Select(kvp => kvp.Key)
      );

    var expectTraverse = new Base() { id = "Dict Member" };
    var expectIgnored = new Base() { id = "Not Dict Member" };

    TraversalMock testCase = new TraversalMock()
    {
      ListChildren = new List<Base>() { expectIgnored },
      DictChildren = new Dictionary<string, Base>() { ["myprop"] = expectTraverse },
      Child = expectIgnored,
    };

    var ret = Traverse(testCase, traverseListsRule).Select(b => b.current).ToList();

    //Assert expected members present
    Assert.That(ret, Has.Exactly(1).Items.EqualTo(testCase));
    Assert.That(ret, Has.Exactly(1).Items.EqualTo(expectTraverse));
    
    //Assert unexpected members not present
    Assert.That(ret, Has.No.Member(expectIgnored));
    Assert.That(ret, Has.Count.EqualTo(2));
  }
  
  [Test]
  public void Traverse_TraversesDynamic()
  {
    var traverseListsRule = TraversalRule.NewTraveralRule()
      .When(_ => true)
      .ContinueTraversing(x => x.GetMembers(DynamicBaseMemberType.Dynamic)
        .Select(kvp => kvp.Key)
      );

    var expectTraverse = new Base() { id = "List Member" };
    var expectIgnored = new Base() { id = "Not List Member" };

    TraversalMock testCase = new TraversalMock()
    {
      Child = expectIgnored,
      ["dynamicChild"] = expectTraverse,
      ["dynamicListChild"] = new List<Base>{ expectTraverse }
    };

    var ret = Traverse(testCase, traverseListsRule).Select(b => b.current).ToList();

    //Assert expected members present
    Assert.That(ret, Has.Exactly(1).Items.EqualTo(testCase));
    Assert.That(ret, Has.Exactly(2).Items.EqualTo(expectTraverse));
    
    //Assert unexpected members not present
    Assert.That(ret, Has.No.Member(expectIgnored));
    Assert.That(ret, Has.Count.EqualTo(3));
  }
  
  
  [Test]
  public void Traverse_ExclusiveRule()
  {
    var expectTraverse = new Base() { id = "List Member" };
    var expectIgnored = new Base() { id = "Not List Member" };
    
    var traverseListsRule = TraversalRule.NewTraveralRule()
      .When(_ => true)
      .ContinueTraversing(x => x.GetMembers(DynamicBaseMemberType.Dynamic)
        .Select(kvp => kvp.Key)
      );


    TraversalMock testCase = new TraversalMock()
    {
      Child = expectIgnored,
      ["dynamicChild"] = expectTraverse,
      ["dynamicListChild"] = new List<Base>{ expectTraverse }
    };

    var ret = Traverse(testCase, traverseListsRule).Select(b => b.current).ToList();

    //Assert expected members present
    Assert.That(ret, Has.Exactly(1).Items.EqualTo(testCase));
    Assert.That(ret, Has.Exactly(2).Items.EqualTo(expectTraverse));
    
    //Assert unexpected members not present
    Assert.That(ret, Has.No.Member(expectIgnored));
    Assert.That(ret, Has.Count.EqualTo(3));
  }
  
  
  
  
  
  
  
  
  //
  //
  //
  //
  // [TestCaseSource(nameof(TestCasesAll))]
  // public void Traverse_ExpectedCount_WhenTraversingAll((Base testCase, int expectedChildren) arg)
  // {
  //   // Setup system under test
  //   var alwaysAll = CreateRuleFor(_ => true, DynamicBaseMemberType.All);
  //   var sut = new GraphTraversal(alwaysAll);
  //   
  //   //Run Test
  //   var test = sut.Traverse(arg.testCase).ToList();
  //   
  //   //Assert
  //   var expectedChildCount = arg.expectedChildren;
  //   Assert.That(test, Has.Count.EqualTo(expectedChildCount));
  // }
  //
  // [TestCaseSource(nameof(TestCasesDynamic))]
  // public void Traverse_ExpectedCount_WhenTraversingDynamic((Base testCase, int expectedChildren) arg)
  // {
  //   // Setup system under test
  //   var alwaysAll = CreateRuleFor(_ => true, DynamicBaseMemberType.Dynamic);
  //   var sut = new GraphTraversal(alwaysAll);
  //   
  //   //Run Test
  //   var test = sut.Traverse(arg.testCase).ToList();
  //   
  //   //Assert
  //   var expectedChildCount = arg.expectedChildren;
  //   Assert.That(test, Has.Count.EqualTo(expectedChildCount + 1)); // + 1 for the root `testCase` object
  // }
  //
  //
  //
  // private static ITraversalRule CreateRuleFor(WhenCondition when, DynamicBaseMemberType members)
  // {
  //   return TraversalRule.NewTraveralRule()
  //     .When(when)
  //     .ContinueTraversing(x => x.GetMembers(members).Keys);
  // }
  //
  // private static (Base, int)[] TestCasesAll()
  // {
  //   bool CountAll(string _) => true;
  //   return new (Base, int)[]
  //   {
  //     new TestCaseGenerator(CountAll).GenerateTestCase(0),
  //     new TestCaseGenerator(CountAll).GenerateTestCase(1),
  //     new TestCaseGenerator(CountAll).GenerateTestCase(4),
  //   };
  // }
  //
  // private static (Base, int)[] TestCasesDynamic()
  // {
  //   bool CountDynamic(string name) => name.ToLower().Contains("dynamic");
  //   return new (Base, int)[]
  //   {
  //     //new TestCaseGenerator(CountDynamic).GenerateTestCase(0),
  //     new TestCaseGenerator(CountDynamic).GenerateTestCase(1),
  //     //new TestCaseGenerator(CountDynamic).GenerateTestCase(4),
  //   };
  // }
  //
  // private class TestCaseGenerator
  // {
  //   private int count = 0;
  //
  //   private Func<string, bool> shouldCountFunc;
  //
  //   public TestCaseGenerator(Func<string, bool> shouldCountFunc)
  //   {
  //     this.shouldCountFunc = shouldCountFunc;
  //   }
  //   
  //   public (Base testCase, int expectedChildren) GenerateTestCase(int depth)
  //   {
  //     string N(string name)
  //     {
  //       string ret = $"{depth}{name}";
  //       if(shouldCountFunc(ret)) count++;
  //       return ret;
  //     } 
  //     
  //     Base b = new TraversalMock()
  //     {
  //       id = N(depth.ToString()),
  //       Child = depth > 0 ? GenerateTestCase(depth - 1).testCase : null,
  //       ["dynamicChild"] = new Base() { id = N("dynamicChildPayload") },
  //       ObjectChild = new Base() { id = N("objectChildPayload") },
  //       ListChildren =
  //         new()
  //         {
  //           new() { id = N("listChildPayload0") },
  //           new() { id = N("listChildPayload1") },
  //           new() { id = N("listChildPayload2") },
  //         },
  //       NestedListChildren = new()
  //       {
  //         new()
  //         {
  //           new() { id = N("nestedListChild00") },
  //           new() { id = N("listChildPayload01") },
  //           new() { id = N("listChildPayload02") },
  //         },
  //         new()
  //         {
  //           new() { id = N("nestedListChild10") }, 
  //           new() { id = N("listChildPayload11") },
  //         },
  //       },
  //       DictChildren = new Dictionary<string, Base>()
  //       {
  //         [$"{depth}first"] = new() { id = N("dict1") },
  //         [$"{depth}second"] = new() { id = N("dict2") },
  //         [$"{depth}third"] = new() { id = N("dict3") },
  //       }
  //     };
  //     return (b, count);
  //   }
  // }

}
