#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Speckle.Core.Models;
using Autodesk.Revit.DB;
using Speckle.Core.Logging;
using Speckle.Core.Models.Extensions;
using Splat;

namespace Speckle.ConnectorRevit;

public sealed class RevitCommitObjectBuilder : CommitObjectBuilder
{
  private const string Types = "Types";
  private const string Elements = nameof(Collection.elements);

  private readonly bool byLevel;
  
  public RevitCommitObjectBuilder(bool byLevel)
  {
    this.byLevel = byLevel;
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

    
    string collectionId;
    string collectionType;
    string collectionName;
    if (byLevel)
    {
      var level = GetLevel(revitElement);
      collectionId = level.UniqueId;
      collectionName = level.Name;
      collectionType = "Revit Level";
    }
    else
    {
      collectionId = GetCategoryId(conversionResult, revitElement);
      collectionName = collectionId;
      collectionType = "Revit Category";
    }
      
    
    Element? host = GetHost(revitElement);
    
    // In order of priority, we want to try and nest under the host (if it exists, and was converted) otherwise, fallback to category.
    AddRelationship(conversionResult, (host?.UniqueId, Elements), (collectionId, Elements));

    // Create collection if not already, ensure it gets added to the root object.
    if (!converted.ContainsKey(collectionId))
    {
      Collection collection = new(collectionName, collectionType) { applicationId = collectionId };
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
