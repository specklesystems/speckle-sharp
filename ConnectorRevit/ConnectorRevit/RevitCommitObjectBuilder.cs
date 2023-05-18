#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Speckle.Core.Models;
using Autodesk.Revit.DB;

namespace Speckle.ConnectorRevit;

public enum CommitCollectionStrategy
{
  ByLevel,
  ByCollection,
}

public sealed class RevitCommitObjectBuilder : CommitObjectBuilder<Element>
{
  private const string Types = "Types";
  private const string Elements = nameof(Collection.elements);

  private readonly CommitCollectionStrategy _commitCollectionStrategy;

  private readonly IDictionary<string, Collection> _collections = new Dictionary<string, Collection>();

  public RevitCommitObjectBuilder(CommitCollectionStrategy commitCollectionStrategy)
  {
    this._commitCollectionStrategy = commitCollectionStrategy;
  }

  public override void BuildCommitObject(Base rootCommitObject)
  {
    var convertedObjects = converted.Values.ToArray();
    foreach (var col in _collections)
    {
      converted.Add(col.Key, col.Value);
    }
    converted.Add(Types, new());

    // Apply object -> object, and object -> collection relationships
    ApplyRelationships(convertedObjects, rootCommitObject);

    var rootElements = (IList<Base>)(rootCommitObject["elements"] ??= new List<Base>());

    //Finally, apply collection -> host relationships
    foreach (var col in _collections.Values)
    {
      if (!col.elements.Any())
        continue;
      rootElements.Add(col);
    }

    rootCommitObject[$"@{Types}"] = converted[Types];
  }

  public override void IncludeObject(Base conversionResult, Element nativeElement)
  {
    switch (nativeElement)
    {
      // Special case for ElementTyped objects, add them to "Types"
      case ElementType:
      {
        var category = GetCategoryId(conversionResult, nativeElement);
        SetRelationship(conversionResult, (Types, category));
        return;
      }
      // Special cases for non-geometry, we want to nest under the root object, not in a collection
      case View:
      case Level:
      case ProjectInfo:
      case Autodesk.Revit.DB.Material:
        var propName = GetCategoryId(conversionResult, nativeElement);
        SetRelationship(conversionResult, (Root, propName));
        return;
    }

    // Define which collection this element should be nested under
    string collectionId,
      collectionName,
      collectionType;

    switch (_commitCollectionStrategy)
    {
      case CommitCollectionStrategy.ByLevel:
      {
        Level? level = GetLevel(nativeElement);
        collectionId = level?.UniqueId ?? Root;
        collectionName = level?.Name;
        collectionType = "Revit Level";
        break;
      }
      case CommitCollectionStrategy.ByCollection:
        collectionId = GetCategoryId(conversionResult, nativeElement);
        collectionName = collectionId;
        collectionType = "Revit Category";
        break;
      default:
        throw new InvalidOperationException($"No case for {_commitCollectionStrategy}");
    }

    // In order of priority, we want to try and nest under the host (if it exists, and was converted) otherwise, fallback to category.
    Element? host = GetHost(nativeElement);

    if (conversionResult.GetType().Name is "Network")
    {
      //WORKAROUND: we don't support hosting networks.
      host = null;
    }

    // Create collection if not already
    if (!_collections.ContainsKey(collectionId) && collectionId != Root)
    {
      Collection collection = new(collectionName, collectionType) { applicationId = collectionId };
      _collections.Add(collectionId, collection);
    }

    SetRelationship(conversionResult, (host?.UniqueId, Elements), (collectionId, Elements));
  }

  private static string GetCategoryId(Base conversionResult, Element revitElement)
  {
    return conversionResult.GetType().Name switch
    {
      "Network" => "Networks",
      "FreeformElement" => "FreeformElement",
      _ => ConnectorRevitUtils.GetEnglishCategoryName(revitElement.Category)
    };
  }

  private static Level? GetLevel(Element revitElement)
  {
    return revitElement.Document.GetElement(revitElement.LevelId) as Level;
  }

  private static Element? GetHost(Element hostedElement)
  {
    return hostedElement switch
    {
      Autodesk.Revit.DB.FamilyInstance i => i.Host,
      Autodesk.Revit.DB.Opening i => i.Host,
      Autodesk.Revit.DB.DividedSurface i => i.Host,
      Autodesk.Revit.DB.FabricationPart i => i.Document.GetElement(i.GetHostedInfo()?.HostId),
      Autodesk.Revit.DB.DisplacementElement i => i.Document.GetElement(i.ParentId),
      Autodesk.Revit.DB.Architecture.ContinuousRail i => i.Document.GetElement(i.HostRailingId),
      Autodesk.Revit.DB.Architecture.BuildingPad i => i.Document.GetElement(i.HostId),
      Autodesk.Revit.DB.Architecture.Railing i => i.HasHost ? i.Document.GetElement(i.HostId) : null, //TODO: Check if this HasHost is required
#if REVIT2019 || REVIT2020 || REVIT2021 || REVIT2022
      Autodesk.Revit.DB.Structure.LoadBase i => i.HostElement,
#else
      Autodesk.Revit.DB.Structure.LoadBase i => i.IsHosted ? i.Document.GetElement(i.HostElementId) : null, //TODO: Check if this IsHosted is required
#endif
      Autodesk.Revit.DB.Structure.FabricSheet i => i.Document.GetElement(i.HostId),
      Autodesk.Revit.DB.Structure.FabricArea i => i.Document.GetElement(i.HostId),

      _ => null
    };
  }
}
