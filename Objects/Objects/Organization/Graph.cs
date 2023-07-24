#nullable enable
using System;
using System.Collections.Generic;
using Speckle.Core.Models;

namespace Objects.Organization
{
  public class Graph : Base
  {
    public Graph() { }

    public string name { get; set; }

    /// <summary>
    /// Constructor for a basic graph.
    /// </summary>
    /// <param name="name">The human-readable name of this graph</param>
    public Graph(string name)
    { 
      this.name = name;
    }

    [DetachProperty]
    public Dictionary<string, Base> elements { get; set; } = new();

    [DetachProperty]
    public Dictionary<string, Dictionary<string, Base>> referenceMap { get; set; } = new();

    public void AddElement(Base element, Func<string, Base> getElementByAppId)
    {
      if (string.IsNullOrEmpty(element.applicationId))
      {
        throw new ArgumentNullException("ApplicationId of Speckle element cannot be null or empty");
      }

      if (referenceMap.TryGetValue(element.applicationId, out var elementReferenceMap))
      {
        // element with this applicationId was already added
        return;
      }
      else
      {
        elements.Add(element.applicationId, element);
        elementReferenceMap = new();
        referenceMap[element.applicationId] = elementReferenceMap;
      }

      foreach (var prop in element.GetMembers())
      {
        var propName = prop.Key;
        var propValue = prop.Value;

        if (propValue is not ApplicationIdReference applicationIdReference)
        {
          continue;
        }

        elementReferenceMap[applicationIdReference.applicationId] = getElementByAppId(applicationIdReference.applicationId);
      }
    }

    public Base? GetBaseReferencedByProvidedBase(Base element, ApplicationIdReference reference)
    {
      if (!referenceMap.TryGetValue(element.applicationId, out var elementReferenceMap))
      {
        return null;
      }
      if (!elementReferenceMap.TryGetValue(reference.applicationId, out var @base))
      {
        return null;
      }
      return @base;
    }
  }
}
