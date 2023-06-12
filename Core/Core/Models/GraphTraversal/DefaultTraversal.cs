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
      .ContinueTraversing(ElementsAliases);

    return new GraphTraversal(convertableRule, IgnoreResultsRule, DefaultRule);
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

    var displayValueRule = TraversalRule.NewTraversalRule().When(HasDisplayValue).ContinueTraversing(ElementsAliases);

    return new GraphTraversal(convertableRule, displayValueRule, IgnoreResultsRule, DefaultRule);
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

    return new GraphTraversal(bimElementRule, IgnoreResultsRule, DefaultRule);
  }

  //These functions are just meant to make the syntax of defining rules less verbose, they are likely to change frequently/be restructured
  #region Helper Functions

  //WORKAROUND: ideally, traversal rules would not have Objects specific rules.
  private static readonly ITraversalRule IgnoreResultsRule = TraversalRule
    .NewTraversalRule()
    .When(o => o.speckle_type.Contains("Objects.Structural.Results"))
    .ContinueTraversing(None);

  private static readonly ITraversalRule DefaultRule = TraversalRule
    .NewTraversalRule()
    .When(_ => true)
    .ContinueTraversing(Members());

  internal static readonly string[] elementsPropAliases = { "elements", "@elements" };

  internal static IEnumerable<string> ElementsAliases(Base _)
  {
    return elementsPropAliases;
  }

  public static IEnumerable<string> None(Base _)
  {
    return Enumerable.Empty<string>();
  }

  public static SelectMembers Members(DynamicBaseMemberType includeMembers = DynamicBase.DefaultIncludeMembers)
  {
    return x => x.GetMembers(includeMembers).Keys;
  }

  internal static readonly string[] displayValueAliases = { "displayValue", "@displayValue" };

  internal static bool HasDisplayValue(Base x)
  {
    return displayValueAliases.Any(m => x[m] != null);
  }

  #endregion
}
