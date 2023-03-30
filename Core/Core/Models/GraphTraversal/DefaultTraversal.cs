using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
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
        .ContinueTraversing(b =>
        {
          var membersToTraverse = b.GetDynamicMembers()
            .Concat(displayValueAliases)
            .Concat(elementsAliases)
            .Except(ignoreProps);
          return membersToTraverse;
        });

      var ignoreResultsRule = TraversalRule.NewTraversalRule()
        .When(o => o.speckle_type.Contains("Objects.Structural.Results"))
        .ContinueTraversing(None);

      var defaultRule = TraversalRule.NewTraversalRule()
        .When(_ => true)
        .ContinueTraversing(Members());

      return new GraphTraversal(convertableRule, ignoreResultsRule, defaultRule);
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
        .ContinueTraversing(b => b.GetDynamicMembers()
          .Concat(displayValueAliases)
          .Except(elementsAliases)
          .Except(ignoreProps)
        );

      //WORKAROUND: ideally, traversal rules would not have Objects specific rules.  
      var ignoreResultsRule = TraversalRule.NewTraversalRule()
        .When(o => o.speckle_type.Contains("Objects.Structural.Results"))
        .ContinueTraversing(None);

      var defaultRule = TraversalRule.NewTraversalRule()
        .When(_ => true)
        .ContinueTraversing(Members());

      return new GraphTraversal(convertableRule, displayValueRule, ignoreResultsRule, defaultRule);
    }



    //These functions are just meant to make the syntax of defining rules less verbose, they are likely to change frequently/be restructured
    #region Helper Functions

    internal static readonly string[] elementsAliases = { "elements", "@elements" };
    internal static IEnumerable<string> ElementsAliases(Base _) => elementsAliases;

    internal static readonly string[] displayValueAliases = { "displayValue", "@displayValue" };
    internal static readonly string[] ignoreProps = new[] { "@blockDefinition" }.Concat(displayValueAliases).ToArray();
    internal static IEnumerable<string> DisplayValueAliases(Base _) => displayValueAliases;
    internal static IEnumerable<string> None(Base _) => Enumerable.Empty<string>();
    internal static SelectMembers Members(DynamicBaseMemberType includeMembers = DynamicBase.DefaultIncludeMembers) => x => x.GetMembers(includeMembers).Keys;
    internal static SelectMembers DynamicMembers() => x => x.GetDynamicMembers();
    internal static SelectMembers Concat(params SelectMembers[] selectProps) => x => selectProps.SelectMany(i => i.Invoke(x));
    internal static SelectMembers Except(SelectMembers selectProps, IEnumerable<string> excludeProps) => x => selectProps.Invoke(x).Except(excludeProps);
    internal static bool HasElements(Base x) => elementsAliases.Any(m => x[m] != null);
    internal static bool HasDisplayValue(Base x) => displayValueAliases.Any(m => x[m] != null);

    #endregion
  }
}
