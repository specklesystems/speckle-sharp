using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using DesktopUI2.Models;
using RevitSharedResources.Interfaces;
using Speckle.Core.Models;

namespace ConnectorRevit.Storage
{
  public class StreamStateCache : IReceivedObjectsCache
  {
    private StreamState streamState;
    private Dictionary<string, ApplicationObject> previousContextObjects;
    public StreamStateCache(StreamState state)
    {
      streamState = state;
      var previousObjects = state.ReceivedObjects;
      previousContextObjects = new(previousObjects.Count);
      foreach (var ao in previousObjects)
      {
        var key = ao.applicationId ?? ao.OriginalId;
        if (previousContextObjects.ContainsKey(key))
          continue;
        previousContextObjects.Add(key, ao);
      }
    }

    public void AddConvertedElements(IConvertedObjectsCache convertedObjects)
    {
      var newContextObjects = new List<ApplicationObject>();
      foreach (var @base in convertedObjects.GetConvertedBaseObjects())
      {
        var elements = convertedObjects.GetConvertedObjectsFromApplicationId(@base.applicationId);
        newContextObjects.Add(new ApplicationObject(@base.id, @base.speckle_type)
        {
          applicationId = @base.applicationId,
          CreatedIds = elements
            .Where(e => e is Element element)
            .Select(element => ((Element)element).UniqueId)
            .ToList(),
          Converted = elements.ToList()
        });
      }
      streamState.ReceivedObjects = newContextObjects;
    }

    public HashSet<string> GetApplicationIds()
    {
      return previousContextObjects.Keys.ToHashSet();
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
      //previousContextObjects.Remove(applicationId);
    }
  }
}
