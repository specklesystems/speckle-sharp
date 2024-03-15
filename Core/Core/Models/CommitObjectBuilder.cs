using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Speckle.Core.Logging;
using Speckle.Core.Models.Extensions;

namespace Speckle.Core.Models;

/// <summary>
/// Abstract Builder class for a root commit <see cref="Base"/> object.
/// </summary>
/// <typeparam name="TNativeObjectData">The native object data type needed as input for building <see cref="_nestingInstructions"/></typeparam>
/// <remarks>
/// It is designed to be inherited by a host app specific implementation,
/// to give connectors flexibility in constructing their objects.
/// Inheritors should also create some function to add
/// </remarks>
[SuppressMessage(
  "Naming",
  "CA1708:Identifiers should differ by more than case",
  Justification = "Class contains obsolete members that are kept for backwards compatiblity"
)]
public abstract class CommitObjectBuilder<TNativeObjectData>
{
  /// <summary>Special appId symbol for the root object</summary>
  protected const string ROOT = "__Root";
  private const string ELEMENTS = nameof(Collection.elements);

  /// <summary>app id -> base</summary>
  protected IDictionary<string, Base> Converted { get; }

  /// <summary>Base -> NestingInstructions ordered by priority</summary>
  private readonly Dictionary<Base, IList<NestingInstructions>> _nestingInstructions = new();

  protected CommitObjectBuilder()
  {
    Converted = new Dictionary<string, Base>();
  }

  /// <summary>
  /// Given the parameters, builds connector specific <see cref="_nestingInstructions"/>
  /// to be applied when <see cref="BuildCommitObject"/> is called.
  /// </summary>
  /// <param name="conversionResult"></param>
  /// <param name="nativeElement"></param>
  public abstract void IncludeObject(Base conversionResult, TNativeObjectData nativeElement);

  /// <summary>
  /// Iterates through the converted objects applying
  /// </summary>
  /// <remarks>
  /// Can be overriden to adjust exactly which objects get automatically applied,
  /// or to inject additional items into the <see cref="Converted"/> dict that should not be automatically applied.
  /// </remarks>
  /// <param name="rootCommitObject"></param>
  public virtual void BuildCommitObject(Base rootCommitObject)
  {
    ApplyRelationships(Converted.Values, rootCommitObject);
  }

  protected void SetRelationship(Base conversionResult, NestingInstructions nestingInstructions)
  {
    SetRelationship(conversionResult, new List<NestingInstructions> { nestingInstructions });
  }

  /// <summary>
  /// Sets information on how a given object should be nested in the commit tree.
  /// <paramref name="nestingInstructionsList"/> encodes the order in which we should try and nest the given <paramref name="conversionResult"/>
  /// when <see cref="BuildCommitObject"/> is called
  /// </summary>
  /// <param name="conversionResult">The object to be nested</param>
  /// <param name="nestingInstructionsList">Information about how the object ideally should be nested, in order of priority</param>
  protected void SetRelationship(Base conversionResult, IList<NestingInstructions> nestingInstructionsList)
  {
    string? appId = conversionResult.applicationId;
    if (appId != null)
    {
      Converted[appId] = conversionResult;
    }
    _nestingInstructions[conversionResult] = nestingInstructionsList;
  }

  /// <summary>
  /// For each object in <paramref name="toAdd"/>
  /// <inheritdoc cref="ApplyRelationship"/>
  /// </summary>
  /// <param name="toAdd"></param>
  /// <param name="rootCommitObject"></param>
  protected void ApplyRelationships(IEnumerable<Base> toAdd, Base rootCommitObject)
  {
    foreach (Base c in toAdd)
    {
      try
      {
        ApplyRelationship(c, rootCommitObject);
      }
      catch (Exception ex) when (!ex.IsFatal())
      {
        // This should never happen, we should be ensuring that at least one of the parents is valid.
        SpeckleLog.Logger.Fatal(ex, "Failed to add object {speckleType} to commit object", c?.GetType());
      }
    }
  }

  /// <summary>
  /// Will attempt to find and nest the <paramref name="current"/> object
  /// under the first valid parent according to the <see cref="_nestingInstructions"/> <see cref="Converted"/> dictionary.
  /// </summary>
  /// <remarks>
  /// A parent is considered valid if
  /// 1. Is non null
  /// 2. Is in the <see cref="Converted"/> dictionary
  /// 3. Has (or can dynamically accept) a <see cref="IList"/> typed property with the propName specified by the <see cref="_nestingInstructions"/> item
  /// 4. Said <see cref="IList"/> can accept the <paramref name="current"/> object's type
  /// </remarks>
  /// <param name="current"></param>
  /// <param name="rootCommitObject"></param>
  /// <exception cref="InvalidOperationException">Thrown when no valid parent was found for <parameref name="current"/> given <see cref="_nestingInstructions"/></exception>
  protected void ApplyRelationship(Base current, Base rootCommitObject)
  {
    var instructions = _nestingInstructions[current];
    foreach (var instruction in instructions)
    {
      if (instruction.ParentApplicationId is null)
      {
        continue;
      }

      Base? parent;
      if (instruction.ParentApplicationId == ROOT)
      {
        parent = rootCommitObject;
      }
      else
      {
        Converted.TryGetValue(instruction.ParentApplicationId, out parent);
      }

      if (parent is null)
      {
        continue;
      }

      try
      {
        instruction.Nest(parent, current);
        return;
      }
      catch (Exception ex) when (!ex.IsFatal())
      {
        // A parent was found, but it was invalid (Likely because of a type mismatch on a `elements` property)
        SpeckleLog.Logger.Warning(ex, "Failed to add object {speckleType} to a converted parent", current.GetType());
      }
    }

    throw new InvalidOperationException(
      $"Could not find a valid parent for object of type {current.GetType()}. Checked {instructions.Count} potential parent, and non were converted!"
    );
  }

  protected static void NestUnderElementsProperty(Base parent, Base child)
  {
    NestUnderProperty(parent, child, ELEMENTS);
  }

  protected static void NestUnderProperty(Base parent, Base child, string property)
  {
    if (parent.GetDetachedProp(property) is not IList elements)
    {
      elements = new List<Base>();
      parent.SetDetachedProp(property, elements);
    }

    elements.Add(child);
  }

  [Obsolete("Renamed to " + nameof(ROOT))]
  [SuppressMessage("Style", "IDE1006:Naming Styles")]
  protected const string Root = ROOT;

  [Obsolete("Renamed to " + nameof(Converted))]
  [SuppressMessage("Style", "IDE1006:Naming Styles")]
  protected IDictionary<string, Base> converted => Converted;
}
