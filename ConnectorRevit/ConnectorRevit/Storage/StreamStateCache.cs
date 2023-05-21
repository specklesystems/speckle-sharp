using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using RevitSharedResources.Interfaces;
using Speckle.Core.Models;

namespace ConnectorRevit.Storage
{
  public class StreamStateCache : IReceivedObjectsCache
  {
    private Dictionary<string, ApplicationObject> previousContextObjects;
    public StreamStateCache(List<ApplicationObject> previousObjects)
    {
      previousContextObjects = new(previousObjects.Count);
      foreach (var ao in previousObjects)
      {
        var key = ao.applicationId ?? ao.OriginalId;
        if (previousContextObjects.ContainsKey(key))
          continue;
        previousContextObjects.Add(key, ao);
      }
    }
    public void AddReceivedElement(Element element, Base @base)
    {
      previousContextObjects[@base.applicationId] = new ApplicationObject(@base.id, @base.speckle_type) 
      { 
        applicationId = @base.applicationId,
        CreatedIds = new List<string> { element.UniqueId },
        Converted = new List<object> { element },
      };
    }
    public void AddReceivedElements(List<Element> elements, Base @base)
    {
      previousContextObjects[@base.applicationId] = new ApplicationObject(@base.id, @base.speckle_type)
      {
        applicationId = @base.applicationId,
        CreatedIds = elements.Select(e => e.UniqueId).ToList(),
        Converted = elements.Cast<object>().ToList(),
      };
    }

    public IEnumerable<string> GetApplicationIds()
    {
      foreach (var kvp in previousContextObjects)
      {
        yield return kvp.Value.applicationId;
      }
    }

    public Element? GetExistingElementFromApplicationId(Document doc, string applicationId)
    {
      if (previousContextObjects.TryGetValue(applicationId, out var appObj))
      {
        //return the cached object, if it's still in the model
        if (appObj.CreatedIds.Any()) return doc.GetElement(appObj.CreatedIds.First());
      }

      //element was not cached in a PreviousContex but might exist in the model
      //eg: user sends some objects, moves them, receives them 
      return doc.GetElement(applicationId);
    }

    public IEnumerable<Element?> GetExistingElementsFromApplicationId(Document doc, string applicationId)
    {
      if (previousContextObjects.TryGetValue(applicationId, out var appObj))
      {
        //return the cached object, if it's still in the model
        foreach (var id in appObj.CreatedIds)
        {
          yield return doc.GetElement(id);
        }
      }

      //element was not cached in a PreviousContex but might exist in the model
      //eg: user sends some objects, moves them, receives them 
      yield return doc.GetElement(applicationId);
    }

    public void RemoveSpeckleId(string applicationId)
    {
      previousContextObjects.Remove(applicationId);
    }
  }
}
