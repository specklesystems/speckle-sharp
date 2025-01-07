#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Autodesk.Revit.DB;
using Objects.Converter.Revit;
using RevitSharedResources.Interfaces;
using Speckle.Core.Models;

namespace ConverterRevitShared;

public enum CommitCollectionStrategy
{
  ByLevel,
  ByCollection,
}

public sealed class RevitCommitObjectBuilder : CommitObjectBuilder<Element>, IRevitCommitObjectBuilder
{
  private const string Types = "Types";
  private const string MEPNetworks = "MEPNetworks";

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
      {
        continue;
      }

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
        var category = GetCategoryId(conversionResult, nativeElement);
        SetRelationship(conversionResult, new NestingInstructions(Types, (p, c) => NestUnderProperty(p, c, category)));
        return;

      // Special cases for non-geometry, we want to nest under the root object, not in a collection
      case View:
      case Level:
      case ProjectInfo:
      case Autodesk.Revit.DB.Material:
        var propName = GetCategoryId(conversionResult, nativeElement);
        SetRelationship(conversionResult, new NestingInstructions(Root, (p, c) => NestUnderProperty(p, c, propName)));
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

    var nestingInstructions = new List<NestingInstructions>();
    AddNestingInstructionsForMEPElement(nativeElement, nestingInstructions);
    AddNestingInstructionsForHosted(conversionResult, nativeElement, nestingInstructions);
    AddNestingInstructionsForCollection(collectionId, collectionName, collectionType, nestingInstructions);

    SetRelationship(conversionResult, nestingInstructions);
  }

  private void AddNestingInstructionsForCollection(
    string collectionId,
    string collectionName,
    string collectionType,
    List<NestingInstructions> nestingInstructions
  )
  {
    // Create collection if not already
    if (!_collections.ContainsKey(collectionId) && collectionId != Root)
    {
      Collection collection = new(collectionName, collectionType) { applicationId = collectionId };
      _collections.Add(collectionId, collection);
    }

    nestingInstructions.Add(new NestingInstructions(collectionId, NestUnderElementsProperty));
  }

  private static void AddNestingInstructionsForHosted(
    Base conversionResult,
    Element nativeElement,
    List<NestingInstructions> nestingInstructions
  )
  {
    // In order of priority, we want to try and nest under the host (if it exists, and was converted) otherwise, fallback to category.
    Element? host = GetHost(nativeElement);

    if (conversionResult.GetType().Name is "Network")
    {
      //WORKAROUND: we don't support hosting networks.
      host = null;
    }

    nestingInstructions.Add(new NestingInstructions(host?.UniqueId, NestUnderElementsProperty));
  }

  private void AddNestingInstructionsForMEPElement(Element nativeElement, List<NestingInstructions> instructions)
  {
    var mepSystemName = GetMEPSystemName(nativeElement);

    if (string.IsNullOrEmpty(mepSystemName))
    {
      return;
    }

    // Create overall network collection if it doesn't exist
    if (!_collections.ContainsKey(MEPNetworks))
    {
      Collection collection = new(MEPNetworks, MEPNetworks) { applicationId = MEPNetworks };
      _collections.Add(MEPNetworks, collection);
    }

    // Create specific collection for this MEPSystem object and add it to the commitBuilder if it doesn't exist
    if (!converted.ContainsKey(mepSystemName))
    {
      Collection mepSystemCollection = new(mepSystemName, mepSystemName) { applicationId = mepSystemName };
      SetRelationship(mepSystemCollection, new NestingInstructions(MEPNetworks, NestUnderElementsProperty));
    }

    instructions.Add(new NestingInstructions(mepSystemName, NestUnderElementsProperty));
  }

  private static string? GetMEPSystemName(Element element)
  {
    return element switch
    {
      MEPCurve o => o.MEPSystem?.Name,
      FamilyInstance o
        => o.MEPModel?.ConnectorManager?.Connectors?.Size > 0
          ? ConverterRevit.GetParamValue<string>(element, BuiltInParameter.RBS_SYSTEM_NAME_PARAM)
          : null,
      _ => null
    };
  }

  private static string GetCategoryId(Base conversionResult, Element revitElement)
  {
    return conversionResult.GetType().Name switch
    {
      "Network" => "Networks",
      "FreeformElement" => "FreeformElement",
      _ => GetEnglishCategoryName(revitElement.Category)
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
      Autodesk.Revit.DB.FabricationPart i => HandleFabricationPart(i),
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
      Autodesk.Revit.DB.Structure.Rebar i => i.Document.GetElement(i.GetHostId()),
      _ => null
    };
  }

  /// <summary>
  /// Processes a FabricationPart to retrieve its associated host element. FabricationPart class has no HasHost method.
  /// </summary>
  /// <param name="fabricationPart">The FabricationPart to process.</param>
  /// <returns>
  /// The host element of the given FabricationPart, or null if no host is associated.
  /// </returns>
  private static Element? HandleFabricationPart(FabricationPart fabricationPart)
  {
    var hostedInfo = fabricationPart.GetHostedInfo();
    return hostedInfo == null ? null : fabricationPart.Document.GetElement(hostedInfo.HostId);
  }

  /// <summary>
  /// We want to display a user-friendly category names when grouping objects
  /// For this we are simplifying the BuiltIn one as otherwise, by using the display value, we'd be getting localized category names
  /// which would make querying etc more difficult
  /// TODO: deprecate this in favour of model collections
  /// </summary>
  /// <param name="category"></param>
  /// <returns></returns>
  public static string GetEnglishCategoryName(Category category)
  {
    var builtInCategory = (BuiltInCategory)category.Id.IntegerValue;
    var builtInCategoryName = builtInCategory
      .ToString()
      .Replace("OST_IOS", "") //for OST_IOSModelGroups
      .Replace("OST_MEP", "") //for OST_MEPSpaces
      .Replace("OST_", "") //for any other OST_blablabla
      .Replace("_", " ");
    builtInCategoryName = Regex.Replace(builtInCategoryName, "([a-z])([A-Z])", "$1 $2", RegexOptions.Compiled).Trim();
    return builtInCategoryName;
  }
}
