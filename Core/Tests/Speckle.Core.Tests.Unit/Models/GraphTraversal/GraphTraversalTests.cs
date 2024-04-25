using System.Collections;
using NUnit.Framework;
using Speckle.Core.Models;
using Speckle.Core.Models.GraphTraversal;

namespace Speckle.Core.Tests.Unit.Models.GraphTraversal;

[TestFixture, TestOf(typeof(Core.Models.GraphTraversal.GraphTraversal))]
public class GraphTraversalTests
{
  private static IEnumerable<TraversalContext> Traverse(Base testCase, params ITraversalRule[] rules)
  {
    var sut = new Core.Models.GraphTraversal.GraphTraversal(rules);
    return sut.Traverse(testCase);
  }

  [Test]
  public void Traverse_TraversesListMembers()
  {
    var traverseListsRule = TraversalRule
      .NewTraversalRule()
      .When(_ => true)
      .ContinueTraversing(
        x => x.GetMembers(DynamicBaseMemberType.All).Where(p => p.Value is IList).Select(kvp => kvp.Key)
      );

    var expectTraverse = new Base { id = "List Member" };
    var expectIgnored = new Base { id = "Not List Member" };

    TraversalMock testCase =
      new()
      {
        ListChildren = new List<Base> { expectTraverse },
        DictChildren = new Dictionary<string, Base> { ["myprop"] = expectIgnored },
        Child = expectIgnored
      };

    var ret = Traverse(testCase, traverseListsRule).Select(b => b.Current).ToList();

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
    var traverseListsRule = TraversalRule
      .NewTraversalRule()
      .When(_ => true)
      .ContinueTraversing(
        x => x.GetMembers(DynamicBaseMemberType.All).Where(p => p.Value is IDictionary).Select(kvp => kvp.Key)
      );

    var expectTraverse = new Base { id = "Dict Member" };
    var expectIgnored = new Base { id = "Not Dict Member" };

    TraversalMock testCase =
      new()
      {
        ListChildren = new List<Base> { expectIgnored },
        DictChildren = new Dictionary<string, Base> { ["myprop"] = expectTraverse },
        Child = expectIgnored
      };

    var ret = Traverse(testCase, traverseListsRule).Select(b => b.Current).ToList();

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
    var traverseListsRule = TraversalRule
      .NewTraversalRule()
      .When(_ => true)
      .ContinueTraversing(x => x.GetMembers(DynamicBaseMemberType.Dynamic).Select(kvp => kvp.Key));

    var expectTraverse = new Base { id = "List Member" };
    var expectIgnored = new Base { id = "Not List Member" };

    TraversalMock testCase =
      new()
      {
        Child = expectIgnored,
        ["dynamicChild"] = expectTraverse,
        ["dynamicListChild"] = new List<Base> { expectTraverse }
      };

    var ret = Traverse(testCase, traverseListsRule).Select(b => b.Current).ToList();

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
    var expectTraverse = new Base { id = "List Member" };
    var expectIgnored = new Base { id = "Not List Member" };

    var traverseListsRule = TraversalRule
      .NewTraversalRule()
      .When(_ => true)
      .ContinueTraversing(x => x.GetMembers(DynamicBaseMemberType.Dynamic).Select(kvp => kvp.Key));

    TraversalMock testCase =
      new()
      {
        Child = expectIgnored,
        ["dynamicChild"] = expectTraverse,
        ["dynamicListChild"] = new List<Base> { expectTraverse }
      };

    var ret = Traverse(testCase, traverseListsRule).Select(b => b.Current).ToList();

    //Assert expected members present
    Assert.That(ret, Has.Exactly(1).Items.EqualTo(testCase));
    Assert.That(ret, Has.Exactly(2).Items.EqualTo(expectTraverse));

    //Assert unexpected members not present
    Assert.That(ret, Has.No.Member(expectIgnored));
    Assert.That(ret, Has.Count.EqualTo(3));
  }
}
