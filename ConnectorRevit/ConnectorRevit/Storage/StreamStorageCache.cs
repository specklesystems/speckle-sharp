using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using DesktopUI2.Models;
using RevitSharedResources.Interfaces;
using Speckle.Core.Models;

namespace ConnectorRevit.Storage
{
  internal class StreamStateCache : IReceivedObjectsCache
  {
    private StreamState streamState { get; }
    private Dictionary<string, ApplicationObject> previousContextObjects;
    public StreamStateCache(StreamState state, List<ApplicationObject> objects)
    {
      this.streamState = state;
      previousContextObjects = new(objects.Count);
      foreach (var ao in objects)
      {
        var key = ao.applicationId ?? ao.OriginalId;
        if (previousContextObjects.ContainsKey(key))
          continue;
        previousContextObjects.Add(key, ao);
      }
    }
    public void AddReceivedElement(Element element, Base @base)
    {
      previousContextObjects.Add(@base.applicationId, new ApplicationObject(@base.id, @base.speckle_type) 
      { 
        applicationId = @base.applicationId,
        CreatedIds = new List<string> { element.UniqueId },
        Converted = new List<object> { element },
      });
    }
    public void AddReceivedElements(List<Element> elements, Base @base)
    {
      previousContextObjects.Add(@base.applicationId, new ApplicationObject(@base.id, @base.speckle_type)
      {
        applicationId = @base.applicationId,
        CreatedIds = elements.Select(e => e.UniqueId).ToList(),
        Converted = elements.Cast<object>().ToList(),
      });
    }

    public ICollection<string> GetAllApplicationIds(Document doc)
    {
      throw new NotImplementedException();
    }

    public IEnumerable<string> GetApplicationIds(Document doc, string streamId)
    {
      foreach (var kvp in previousContextObjects)
      {
        yield return kvp.Value.applicationId;
      }
    }

    public Element GetExistingElementFromApplicationId(Document doc, string applicationId)
    {
      Element element = null;
      if (!previousContextObjects.ContainsKey(applicationId))
      {
        //element was not cached in a PreviousContex but might exist in the model
        //eg: user sends some objects, moves them, receives them 
        element = doc.GetElement(applicationId);
      }
      else
      {
        var @ref = previousContextObjects[applicationId];
        //return the cached object, if it's still in the model
        if (@ref.CreatedIds.Any())
          element = doc.GetElement(@ref.CreatedIds.First());
      }

      return element;
    }

    public IEnumerable<Element?> GetExistingElementsFromApplicationId(Document doc, string applicationId)
    {
      if (!previousContextObjects.ContainsKey(applicationId))
      {
        //element was not cached in a PreviousContex but might exist in the model
        //eg: user sends some objects, moves them, receives them 
        yield return doc.GetElement(applicationId);
      }
      else
      {
        var @ref = previousContextObjects[applicationId];
        //return the cached objects, if they are still in the model
        foreach (var id in @ref.CreatedIds)
        {
          yield return doc.GetElement(id);
        }
      }
    }

    public void RemoveSpeckleId(Document doc, string applicationId)
    {
      previousContextObjects.Remove(applicationId);
    }

    public void Save()
    {
      // not needed here but I suspect will be needed for different 
      // don't talk to me about the interface segragation principle
      // I invented the inteface segregation principle
    }
  }
}
