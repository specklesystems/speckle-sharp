using System.Collections.Generic;

#nullable enable
namespace Speckle.Core.Models.GraphTraversal
{
  public delegate bool WhenCondition(Base o); 
  public interface ITraversalBuilderWhen
  {
    ITraversalBuilderTraverse When(WhenCondition condition);
  }
  
  public delegate IEnumerable<string> SelectMembers(Base o); 
  public interface ITraversalBuilderTraverse : ITraversalBuilderWhen
  {
    ITraversalRule ContinueTraversing(SelectMembers members);
  }
  
  public sealed class TraversalRule 
    : ITraversalRule, 
      ITraversalBuilderWhen,
      ITraversalBuilderTraverse
  {
    private List<WhenCondition> conditions;
    private SelectMembers trueAction;

    private TraversalRule()
    {
      conditions = new List<WhenCondition>();
    }

    bool ITraversalRule.DoesRuleHold(Base o)
    {
      foreach (var condition in conditions)
      {
        if (condition.Invoke(o)) return true;
      }
      return false;
    }

    IEnumerable<string> ITraversalRule.MembersToTraverse(Base o)
    {
      return trueAction(o);
    }

    public ITraversalBuilderTraverse When(WhenCondition condition)
    {
      conditions.Add(condition);
      return this;
    }

    public ITraversalRule ContinueTraversing(SelectMembers members)
    {
      this.trueAction = members;
      return this;
    }
    
    public static ITraversalBuilderWhen NewTraveralRule()
    {
      return new TraversalRule();
    }
  }
  
}
