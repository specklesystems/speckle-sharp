#nullable enable
using System.Collections.Generic;
using System.Linq;
using Speckle.Core.Kits;

namespace Speckle.Core.Models.GraphTraversal;

public static class DefaultTraversal
{
  /// <summary>
  /// Default traversal rule that ideally should be used by all connectors
  /// </summary>
  /// <remarks>
  /// Treats convertable objects <see cref="ISpeckleConverter.CanConvertToNative"/> and objects with displayValues as "convertable" such that only elements and dynamic props will be traversed
  /// </remarks>
  /// <param name="converter"></param>
  /// <returns></returns>
  public static GraphTraversal CreateTraverseFunc(ISpeckleConverter converter)
  {
    var convertableRule = TraversalRule
      .NewTraversalRule()
      .When(converter.CanConvertToNative)
      .When(HasDisplayValue)
      .ContinueTraversing(b =>
      {
        var membersToTraverse = b.GetDynamicMembers().Concat(elementsAliases).Except(ignoreProps);
        return membersToTraverse;
      });

    var ignoreResultsRule = TraversalRule
      .NewTraversalRule()
      .When(o => o.speckle_type.Contains("Objects.Structural.Results"))
      .ContinueTraversing(None);

    var defaultRule = TraversalRule.NewTraversalRule().When(_ => true).ContinueTraversing(Members());

    return new GraphTraversal(convertableRule, ignoreResultsRule, defaultRule);
  }

  /// <summary>
  /// Traverses until finds a convertable object then HALTS deeper traversal
  /// </summary>
  /// <remarks>
  /// Current <see cref="Objects.Converter.Revit.ConverterRevit"/> does traversal,
  /// so this traversal is a shallow traversal for directly convertable objects,
  /// and a deep traversal for all other types
  /// </remarks>
  /// <param name="converter"></param>
  /// <returns></returns>
  public static GraphTraversal CreateRevitTraversalFunc(ISpeckleConverter converter)
  {
    var convertableRule = TraversalRule.NewTraversalRule().When(converter.CanConvertToNative).ContinueTraversing(None);

    var displayValueRule = TraversalRule
      .NewTraversalRule()
      .When(HasDisplayValue)
      .ContinueTraversing(b =>
      {
        var membersToTraverse = b.GetDynamicMembers()
          .Concat(elementsAliases)
          .Except(ignoreProps)
          .Concat(displayValueAliases);
        return membersToTraverse;
      });

    //WORKAROUND: ideally, traversal rules would not have Objects specific rules.
    var ignoreResultsRule = TraversalRule
      .NewTraversalRule()
      .When(o => o.speckle_type.Contains("Objects.Structural.Results"))
      .ContinueTraversing(None);

    var defaultRule = TraversalRule.NewTraversalRule().When(_ => true).ContinueTraversing(Members());

    return new GraphTraversal(convertableRule, displayValueRule, ignoreResultsRule, defaultRule);
  }

  /// <summary>
  /// Traverses until finds a convertable object (or fallback) then traverses members
  /// </summary>
  /// <param name="converter"></param>
  /// <returns></returns>
  public static GraphTraversal CreateBIMTraverseFunc(ISpeckleConverter converter)
  {
    var bimElementRule = TraversalRule
      .NewTraversalRule()
      .When(converter.CanConvertToNative)
      .ContinueTraversing(ElementsAliases);

    //WORKAROUND: ideally, traversal rules would not have Objects specific rules.
    var ignoreResultsRule = TraversalRule
      .NewTraversalRule()
      .When(o => o.speckle_type.Contains("Objects.Structural.Results"))
      .ContinueTraversing(None);

    var defaultRule = TraversalRule.NewTraversalRule().When(_ => true).ContinueTraversing(Members());

    return new GraphTraversal(bimElementRule, ignoreResultsRule, defaultRule);
  }

  //These functions are just meant to make the syntax of defining rules less verbose, they are likely to change frequently/be restructured

  #region Helper Functions

  internal static readonly string[] elementsAliases = { "elements", "@elements" };

  internal static IEnumerable<string> ElementsAliases(Base _)
  {
    return elementsAliases;
  }

  internal static readonly string[] displayValueAliases = { "displayValue", "@displayValue" };

  internal static readonly string[] ignoreProps = new[] { "@blockDefinition" }.Concat(displayValueAliases).ToArray();

  internal static IEnumerable<string> DisplayValueAliases(Base _)
  {
    return displayValueAliases;
  }

  internal static IEnumerable<string> None(Base _)
  {
    return Enumerable.Empty<string>();
  }

  internal static SelectMembers Members(DynamicBaseMemberType includeMembers = DynamicBase.DefaultIncludeMembers)
  {
    return x => x.GetMembers(includeMembers).Keys;
  }

  internal static SelectMembers DynamicMembers()
  {
    return x => x.GetDynamicMembers();
  }

  internal static SelectMembers Concat(params SelectMembers[] selectProps)
  {
    return x => selectProps.SelectMany(i => i.Invoke(x));
  }

  internal static SelectMembers Except(SelectMembers selectProps, IEnumerable<string> excludeProps)
  {
    return x => selectProps.Invoke(x).Except(excludeProps);
  }

  internal static bool HasElements(Base x)
  {
    return elementsAliases.Any(m => x[m] != null);
  }

  internal static bool HasDisplayValue(Base x)
  {
    return displayValueAliases.Any(m => x[m] != null);
  }

  #endregion
}
