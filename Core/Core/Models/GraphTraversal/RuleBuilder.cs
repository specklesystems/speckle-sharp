using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;

#nullable enable
namespace Speckle.Core.Models.GraphTraversal
{
  public delegate bool WhenCondition(Base o); 
  public interface ITraversalBuilderWhen
  {
    ITraversalBuilderTraverse When(WhenCondition condition);
  }
  
  public delegate IEnumerable<string> SelectProps(Base o); 
  public interface ITraversalBuilderTraverse : ITraversalBuilderWhen
  {
    ITraversalRule ContinueTraversing(SelectProps props);
  }
  
  public sealed class TraversalRule 
    : ITraversalRule, 
      ITraversalBuilderWhen,
      ITraversalBuilderTraverse
  {
    private List<WhenCondition> conditions;
    private SelectProps trueAction;

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

    public ITraversalRule ContinueTraversing(SelectProps props)
    {
      this.trueAction = props;
      return this;
    }
    
    public static ITraversalBuilderWhen NewTraveralRule()
    {
      return new TraversalRule();
    }
  }


  public class Example
  {

    public bool CanConvertToNative(Base b)
    {
      return false;
    }
    
    public void foo()
    {
      IEnumerable<string> OnlyElements(Base x) => new[] { "elements", "element" };
      IEnumerable<string> AllMembers(Base x) => x.GetMembers().Keys;
      IEnumerable<string> DynamicOnly(Base x) => x.GetMembers(DynamicBaseMemberType.Dynamic).Keys;
      
      var Convertable = TraversalRule.NewTraveralRule()
        .When(x => x.GetMembers().Keys.Contains("displayValue") )
        .ContinueTraversing(OnlyElements);
      
       var Ignore = TraversalRule.NewTraveralRule()
         .When(x => x.speckle_type.ToLower().Contains("organisation"))
         .ContinueTraversing(x => Array.Empty<string>());
      
      var Else = TraversalRule.NewTraveralRule()
        .When(x => true)
        .ContinueTraversing(DynamicOnly);
      
      var traverseFunction = new GraphTraversal(Convertable, Ignore, Else);

      Base myBase = new Base();

      foreach (var @base in traverseFunction.Traverse(myBase))
      {
        
      }
    }


    public void RecursivlyConvert()
    {
      Directory<string, 
      
      
    }
    
  }
}
