using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Linq;
using Speckle.Core.Kits;

namespace Speckle.Core.Models.GraphTraversal;

[SuppressMessage(
  "Naming",
  "CA1708:Identifiers should differ by more than case",
  Justification = "Class contains obsolete members that are kept for backwards compatiblity"
)]
public static class DefaultTraversal
{
  public static GraphTraversal CreateTraversalFunc()
  {
    var convertableRule = TraversalRule
      .NewTraversalRule()
      .When(b => b.GetType() != typeof(Base))
      .When(HasDisplayValue)
      .ContinueTraversing(_ => ElementsPropAliases);

    return new GraphTraversal(convertableRule, s_ignoreResultsRule, DefaultRule.ShouldReturnToOutput(false));
  }

  //These functions are just meant to make the syntax of defining rules less verbose, they are likely to change frequently/be restructured
  #region Helper Functions

  //WORKAROUND: ideally, traversal rules would not have Objects specific rules.
  private static readonly ITraversalRule s_ignoreResultsRule = TraversalRule
    .NewTraversalRule()
    .When(o => o.speckle_type.Contains("Objects.Structural.Results"))
    .ContinueTraversing(None);

  public static ITraversalBuilderReturn DefaultRule =>
    TraversalRule.NewTraversalRule().When(_ => true).ContinueTraversing(Members());

  public static readonly IReadOnlyList<string> ElementsPropAliases = new[] { "elements", "@elements" };

  [Pure]
  public static IEnumerable<string> ElementsAliases(Base _)
  {
    return ElementsPropAliases;
  }

  public static bool HasElements(Base x)
  {
    return ElementsPropAliases.Any(m => x[m] != null);
  }

  public static readonly IReadOnlyList<string> DefinitionPropAliases = new[] { "definition", "@definition" };

  [Pure]
  public static IEnumerable<string> DefinitionAliases(Base _)
  {
    return DefinitionPropAliases;
  }

  public static bool HasDefinition(Base x)
  {
    return DefinitionPropAliases.Any(m => x[m] != null);
  }

  public static readonly IReadOnlyList<string> DisplayValuePropAliases = new[] { "displayValue", "@displayValue" };

  [Pure]
  public static IEnumerable<string> DisplayValueAliases(Base _)
  {
    return DisplayValuePropAliases;
  }

  public static bool HasDisplayValue(Base x)
  {
    return DisplayValuePropAliases.Any(m => x[m] != null);
  }

  public static readonly IReadOnlyList<string> GeometryPropAliases = new[] { "geometry", "@geometry" };

  [Pure]
  public static IEnumerable<string> GeometryAliases(Base _)
  {
    return GeometryPropAliases;
  }

  public static bool HasGeometry(Base x)
  {
    return GeometryPropAliases.Any(m => x[m] != null);
  }

  [Pure]
  public static IEnumerable<string> None(Base _)
  {
    return Enumerable.Empty<string>();
  }

  internal static SelectMembers Members(DynamicBaseMemberType includeMembers = DynamicBase.DEFAULT_INCLUDE_MEMBERS)
  {
    return x => x.GetMembers(includeMembers).Keys;
  }

  public static readonly string[] DisplayValueAndElementsPropAliases = DisplayValuePropAliases
    .Concat(ElementsPropAliases)
    .ToArray();

  [Pure]
  public static IEnumerable<string> DisplayValueAndElementsAliases(Base _)
  {
    return DisplayValueAndElementsPropAliases;
  }

  #endregion

  #region Legacy function varients

  [Obsolete("Renamed to " + nameof(ElementsPropAliases))]
  [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Obsolete")]
  public static IReadOnlyList<string> elementsPropAliases => ElementsPropAliases;

  [Obsolete("Renamed to " + nameof(DisplayValuePropAliases))]
  [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Obsolete")]
  public static IReadOnlyList<string> displayValuePropAliases => DisplayValuePropAliases;

  [Obsolete("Renamed to " + nameof(DefinitionPropAliases))]
  [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Obsolete")]
  public static IReadOnlyList<string> definitionPropAliases => DefinitionPropAliases;

  [Obsolete("Renamed to " + nameof(GeometryPropAliases))]
  [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Obsolete")]
  public static IReadOnlyList<string> geometryPropAliases => GeometryPropAliases;

  [Obsolete("Renamed to " + nameof(DisplayValueAndElementsPropAliases))]
  [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Obsolete")]
  [SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "Obsolete")]
  public static string[] displayValueAndElementsPropAliases => DisplayValueAndElementsPropAliases;

  /// <summary><inheritdoc cref="CreateLegacyTraverseFunc"/></summary>
  /// <remarks><inheritdoc cref="CreateLegacyTraverseFunc"/></remarks>
  /// <param name="converter"></param>
  /// <returns></returns>
  [Obsolete($"Consider using {nameof(CreateTraversalFunc)}")]
  public static GraphTraversal CreateTraverseFunc(ISpeckleConverter converter)
  {
    return CreateLegacyTraverseFunc(converter.CanConvertToNative);
  }

  /// <summary>
  /// Legacy traversal rule that was dependent on the converter
  /// </summary>
  /// <remarks>
  /// Treats convertable objects <see cref="ISpeckleConverter.CanConvertToNative"/> and objects with displayValues as "convertable" such that only elements and dynamic props will be traversed
  /// New code should use <see cref="CreateTraversalFunc"/> instead.
  /// </remarks>
  /// <param name="canConvertToNative"></param>
  /// <returns></returns>
  [Obsolete($"Consider using {nameof(CreateTraversalFunc)}")]
  public static GraphTraversal CreateLegacyTraverseFunc(Func<Base, bool> canConvertToNative)
  {
    var convertableRule = TraversalRule
      .NewTraversalRule()
      .When(b => canConvertToNative(b))
      .When(HasDisplayValue)
      .ContinueTraversing(_ => ElementsPropAliases);

    return new GraphTraversal(convertableRule, s_ignoreResultsRule, DefaultRule);
  }

  /// <summary>
  /// Traverses until finds a convertable object then HALTS deeper traversal
  /// </summary>
  /// <remarks>
  /// The DUI2 Revit connector does traversal,
  /// so this traversal is a shallow traversal for directly convertable objects,
  /// and a deep traversal for all other types
  /// New code should use <see cref="CreateTraversalFunc"/> instead.
  /// </remarks>
  /// <param name="converter"></param>
  /// <returns></returns>
  [Obsolete($"Consider using {nameof(CreateTraversalFunc)}")]
  public static GraphTraversal CreateRevitTraversalFunc(ISpeckleConverter converter)
  {
    var convertableRule = TraversalRule
      .NewTraversalRule()
      .When(converter.CanConvertToNative)
      .When(HasDisplayValue)
      .ContinueTraversing(None);

    return new GraphTraversal(convertableRule, s_ignoreResultsRule, DefaultRule);
  }

  /// <summary>
  /// Traverses until finds a convertable object (or fallback) then traverses members
  /// </summary>
  /// <remarks>
  /// New code should use <see cref="CreateTraversalFunc"/> instead.
  /// </remarks>
  /// <param name="converter"></param>
  /// <returns></returns>
  [Obsolete($"Consider using {nameof(CreateTraversalFunc)}")]
  public static GraphTraversal CreateBIMTraverseFunc(ISpeckleConverter converter)
  {
    var bimElementRule = TraversalRule
      .NewTraversalRule()
      .When(converter.CanConvertToNative)
      .ContinueTraversing(ElementsAliases);

    return new GraphTraversal(bimElementRule, s_ignoreResultsRule, DefaultRule);
  }

  #endregion
}
