#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Speckle.Core.Models;
using Autodesk.Revit.DB;
using Speckle.Core.Logging;
using AppId = System.String;

namespace Speckle.ConnectorRevit;

public sealed class CommitObjectBuilder
{
  private const string Types = "__Types";
  
  public bool ByLevel { get; set; }
  
  private readonly IDictionary<string, Base> converted; //app id -> base
  private readonly IDictionary<Base, AppId> elementHostRelationship; //child -> parent app id
  private readonly IDictionary<Base, AppId> elementCategoryRelationship; //base -> cat name/level id

  public CommitObjectBuilder()
  {
    converted = new Dictionary<string, Base>();
    elementHostRelationship = new Dictionary<Base, string>();
    elementCategoryRelationship = new Dictionary<Base, string>();
  }
  
  public void IncludeObject(
    Base conversionResult,
    ApplicationObject reportObject,
    Element revitElement
  )
  {
    converted.Add(reportObject.applicationId, conversionResult);
    
    string category = conversionResult.GetType().Name switch
    {
      "Network" => "Networks",
      "FreeformElement" => "FreeformElement",
      _ => ConnectorRevitUtils.GetEnglishCategoryName(revitElement.Category)
    };
    
    // Special case for ElementTyped objects, add them to "Types"
    if (revitElement is ElementType)
    {
      elementHostRelationship.Add(conversionResult, Types);
      elementCategoryRelationship.Add(conversionResult, category);
      return;
    }
    

    Element? host = GetHost(revitElement);
    if (host is not null)
    {
      elementHostRelationship.Add(conversionResult, host.UniqueId);
    }
    
    elementCategoryRelationship.Add(conversionResult, category);
  }

  public void BuildCommitObject(Base rootCommitObject)
  {
    Base types = new();
    Dictionary<string, Collection> categories = new();
    
    void AddToHost(Base c, Base host)
    {
      var elements = (IList)(host["elements"]??= new List<Base>()); //TODO: we might want to ensure this is detached for everything!
      elements.Add(c);
    }
    
    void AddToCategory(Base c, string categoryName)
    {
      if (!categories.ContainsKey(categoryName))
      {
        categories.Add(categoryName, new Collection(categoryName, "Revit Category"));
      }
      categories[categoryName].elements.Add(c);
    }

    void AddToTypes(Base c, string categoryName)
    {
      var typesByCat = (IList)(types[categoryName] ??= new List<Base>());
      typesByCat.Add(c);
    }
    
    foreach (Base c in converted.Values)
    {
      try
      {
        if (elementHostRelationship.TryGetValue(c, out string? hostId) //Object has a host
          && converted.TryGetValue(hostId, out Base? host)) //And we converted it
        {
          // Object has a host, and we converted it!
          try
          {
            AddToHost(c, host);
            continue;
          }
          catch (ArgumentException ex)
          {
            // We tried to add it to the host, but it was of the wrong type!
            SpeckleLog.Logger.Warning(ex, "Failed to add object {speckleType} to commit object", c?.GetType());
          }
        }
        
        string categoryName = elementCategoryRelationship[c];
        if (hostId == Types)
        {
          // Special case for ElementTypes
          AddToTypes(c, categoryName);
          continue;
        }

        //Object either had no host, or we didn't convert the host. Nest it under its category
        AddToCategory(c, categoryName);
      }
      catch(Exception ex)
      {
        SpeckleLog.Logger.Error(ex, "Failed to add object {speckleType} to commit object", c?.GetType());
      }
    }
    
    rootCommitObject["Types"] = types;
    rootCommitObject["elements"] = categories.Values.ToList();
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
