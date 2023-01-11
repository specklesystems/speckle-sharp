using System.Collections.Generic;
using System.Linq;
using Speckle.Core.Kits;

#nullable enable
namespace Speckle.Core.Models.GraphTraversal
{
  public static class DefaultTraversal
  {
    
    /// <summary>
    /// Traverses until finds a convertable object (or fallback) then traverses members
    /// </summary>
    /// <param name="converter"></param>
    /// <returns></returns>
    public static GraphTraversal CreateTraverseFunc(ISpeckleConverter converter)
    {
      var convertableRule = TraversalRule.NewTraversalRule()
        .When(converter.CanConvertToNative)
        .When(HasDisplayValue)
        .ContinueTraversing(Except(
          Concat(Members(DynamicBaseMemberType.Dynamic), ElementsAliases), 
          displayValueAliases)
        );

      var defaultRule = TraversalRule.NewTraversalRule()
        .When(_ => true)
        .ContinueTraversing(Members());

      return new GraphTraversal(convertableRule, defaultRule);
    }
    
    /// <summary>
    /// Traverses until finds a convertable object then HALTS deeper traversal
    /// </summary>
    /// <param name="converter"></param>
    /// <returns></returns>
    public static GraphTraversal CreateRevitTraversalFunc(ISpeckleConverter converter)
    {
      var convertableRule = TraversalRule.NewTraversalRule()
        .When(converter.CanConvertToNative)
        .ContinueTraversing(None);
      
      var displayValueRule = TraversalRule.NewTraversalRule()
        .When(HasDisplayValue)
        .ContinueTraversing(Except(
          Concat(Members(DynamicBaseMemberType.Dynamic), ElementsAliases), 
          displayValueAliases)
        );

      var defaultRule = TraversalRule.NewTraversalRule()
        .When(_ => true)
        .ContinueTraversing(Members());

      return new GraphTraversal(convertableRule, defaultRule);
    }

    
    
    //These functions are just meant to make the syntax of defining rules less verbose, they are likely to change frequently/be restructured
    #region Helper Functions

    internal static readonly string[] elementsAliases = { "elements", "@elements" };
    internal static IEnumerable<string> ElementsAliases(Base _) => elementsAliases;

    internal static readonly string[] displayValueAliases = { "displayValue", "@displayValue" };
    internal static IEnumerable<string> DisplayValueAliases(Base _) => displayValueAliases;
    internal static IEnumerable<string> None(Base x) => Enumerable.Empty<string>();
    internal static SelectMembers Members(DynamicBaseMemberType includeMembers = DynamicBase.DefaultIncludeMembers) => x => x.GetMembers(includeMembers).Keys;
    internal static SelectMembers Concat(params SelectMembers[] selectProps) => x => selectProps.SelectMany(i => i.Invoke(x));
    internal static SelectMembers Except(SelectMembers selectProps, IEnumerable<string> excludeProps) => x => selectProps.Invoke(x).Except(excludeProps);
    internal static bool HasElements(Base x) => x.GetMembers().Keys.Any(member => elementsAliases.Contains(member));
    internal static bool HasDisplayValue(Base x) => x.GetMembers().Keys.Any(member => displayValueAliases.Contains(member));
    
    #endregion
  }
}
