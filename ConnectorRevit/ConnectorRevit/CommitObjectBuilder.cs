#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Speckle.Core.Models;
using Autodesk.Revit.DB;
using Speckle.Core.Logging;

namespace Speckle.ConnectorRevit;

public sealed class CommitObjectBuilder
{
  private const string Types = "Types";
  private const string Root = "__Root";
  private const string Elements = nameof(Collection.elements);

  private readonly bool byLayer;
  
  /// <summary>app id -> base </summary>
  private readonly IDictionary<string, Base> converted;
  
  /// <summary>Base -> Tuple{Parent App Id, propName} ordered by priority</summary>
  private readonly IDictionary<Base, IList<(string? parentAppId, string propName)>> parentInfos;

  public CommitObjectBuilder(bool byLayer)
  {
    this.byLayer = byLayer;
    converted = new Dictionary<string, Base>();
    parentInfos = new Dictionary<Base, IList<(string?,string)>>();
  }

  private void AddRelationship(Base conversionResult, params (string? parentAppId, string propName)[] parentInfo)
  {
    converted.Add(conversionResult.applicationId, conversionResult);
    parentInfos.Add(conversionResult, parentInfo );
  }

  public void IncludeObject(
    Base conversionResult,
    Element revitElement
  )
  {
    
    // Special case for ElementTyped objects, add them to "Types"
    if (revitElement is ElementType)
    {
      var category = GetCategoryId(conversionResult, revitElement);
      AddRelationship(conversionResult, (Types, category));
      if (!converted.ContainsKey(Types))
      {
        AddRelationship(new() { applicationId = Types }, (Root, Types));
      }
      return;
    }

    
    string collectionId = GetCategoryId(conversionResult, revitElement);
    
    Element? host = GetHost(revitElement);
    
    // In order of priority, we want to try and nest under the host (if it exists, and was converted) otherwise, fallback to category.
    AddRelationship(conversionResult, (host?.UniqueId, Elements), (collectionId, Elements));

    // Create collection if not already, ensure it gets added to the root object.
    if (!converted.ContainsKey(collectionId))
    {
      Collection collection = new(collectionId, "Revit Category") { applicationId = collectionId };
      AddRelationship(collection, (Root, Elements));
    }
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

  public void BuildCommitObject(Base rootCommitObject)
  {
    foreach (Base c in converted.Values)
    {
      try
      {
        AddToRoot(c, rootCommitObject);
      }
      catch(Exception ex)
      {
        // This should never happen, we should be ensuring that at least one of the parents is valid.
        SpeckleLog.Logger.Fatal(ex, "Failed to add object {speckleType} to commit object", c?.GetType());
      }
    }
  }

  private void AddToRoot(Base current, Base rootCommitObject)
  {
    var parents = parentInfos[current];
    foreach ((string? parentAppId, string propName) in parents)
    {
      if(parentAppId is null) continue;
          
      Base? parent;
      if (parentAppId == Root) parent = rootCommitObject;
      else converted.TryGetValue(parentAppId, out parent);
          
      if(parent is null)
        continue;
          
      try
      {
        var elements = (IList)(parent[propName] ??= new List<Base>()); //TODO: we might want to ensure this is detached for everything!
        elements.Add(current);
        return;
      }
      catch(Exception ex)
      {
        // A parent was found, but it was invalid (Likely because of a type mismatch on a `elements` property)
        SpeckleLog.Logger.Warning(ex, "Failed to add object {speckleType} to a converted parent.", current?.GetType());
      }
    }
    throw new InvalidOperationException($"Could not find a valid parent for object of type {current?.GetType()}. Checked {parents.Count} potential parent, and non were converted!");
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
      Autodesk.Revit.DB.Architecture.Railing i => i.HasHost? i.Document.GetElement(i.HostId) : null, //TODO: Check if this HasHost is required
#if REVIT2019 || REVIT2020 || REVIT2021 || REVIT2022
      Autodesk.Revit.DB.Structure.LoadBase i => i.HostElement,
#else
        Autodesk.Revit.DB.Structure.LoadBase i => i.IsHosted? i.Document.GetElement(i.HostElementId) : null, //TODO: Check if this IsHosted is required
#endif
      Autodesk.Revit.DB.Structure.FabricSheet i => i.Document.GetElement(i.HostId),
      Autodesk.Revit.DB.Structure.FabricArea i => i.Document.GetElement(i.HostId),
        
      _ => null
    };
  }

}
