#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
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
      .ContinueTraversing(_ => elementsPropAliases);

    return new GraphTraversal(convertableRule, IgnoreResultsRule, DefaultRule);
  }

  /// <summary>
  /// Traverses until finds a convertable object then HALTS deeper traversal
  /// </summary>
  /// <remarks>
  /// Current <see cref="Converter{TInput,TOutput}.Revit.ConverterRevit"/> does traversal,
  /// so this traversal is a shallow traversal for directly convertable objects,
  /// and a deep traversal for all other types
  /// </remarks>
  /// <param name="converter"></param>
  /// <returns></returns>
  public static GraphTraversal CreateRevitTraversalFunc(ISpeckleConverter converter)
  {
    var convertableRule = TraversalRule
      .NewTraversalRule()
      .When(converter.CanConvertToNative)
      .When(HasDisplayValue)
      .ContinueTraversing(None);

    return new GraphTraversal(convertableRule, IgnoreResultsRule, DefaultRule);
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

  public static readonly ITraversalRule DefaultRule = TraversalRule
    .NewTraversalRule()
    .When(_ => true)
    .ContinueTraversing(Members());

  public static readonly IReadOnlyList<string> elementsPropAliases = new[] { "elements", "@elements" };

  [Pure]
  public static IEnumerable<string> ElementsAliases(Base _)
  {
    return elementsPropAliases;
  }

  public static bool HasElements(Base x)
  {
    return elementsPropAliases.Any(m => x[m] != null);
  }

  public static readonly IReadOnlyList<string> definitionPropAliases = new[] { "definition", "@definition" };

  [Pure]
  public static IEnumerable<string> DefinitionAliases(Base _)
  {
    return definitionPropAliases;
  }

  public static bool HasDefinition(Base x)
  {
    return definitionPropAliases.Any(m => x[m] != null);
  }

  public static readonly IReadOnlyList<string> displayValuePropAliases = new[] { "displayValue", "@displayValue" };

  [Pure]
  public static IEnumerable<string> DisplayValueAliases(Base _)
  {
    return displayValuePropAliases;
  }

  public static bool HasDisplayValue(Base x)
  {
    return displayValuePropAliases.Any(m => x[m] != null);
  }

  public static readonly IReadOnlyList<string> geometryPropAliases = new[] { "geometry", "@geometry" };

  [Pure]
  public static IEnumerable<string> GeometryAliases(Base _)
  {
    return geometryPropAliases;
  }

  public static bool HasGeometry(Base x)
  {
    return geometryPropAliases.Any(m => x[m] != null);
  }

  [Pure]
  public static IEnumerable<string> None(Base _)
  {
    return Enumerable.Empty<string>();
  }

  internal static SelectMembers Members(DynamicBaseMemberType includeMembers = DynamicBase.DefaultIncludeMembers)
  {
    return x => x.GetMembers(includeMembers).Keys;
  }

  public static readonly string[] displayValueAndElementsPropAliases = displayValuePropAliases
    .Concat(elementsPropAliases)
    .ToArray();

  [Pure]
  public static IEnumerable<string> DisplayValueAndElementsAliases(Base _)
  {
    return displayValueAndElementsPropAliases;
  }

  #endregion
}
