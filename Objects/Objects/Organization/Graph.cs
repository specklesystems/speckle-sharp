#nullable enable
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
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

    public void AddElement(Base element, Func<string, Base> getElementByAppId)
    {
      if (string.IsNullOrEmpty(element.applicationId))
      {
        throw new ArgumentNullException("ApplicationId of Speckle element cannot be null or empty");
      }

      if (!elements.ContainsKey(element.applicationId))
      {
        elements.Add(element.applicationId, element);
      }
    }

    //public GraphNode? GetNodeByAppId(string applicationId)
    //{
    //  if (elements.TryGetValue(applicationId, out var node)) return node;
    //  return null;
    //}

    //[OnDeserialized]
    //internal void OnDeserialized(StreamingContext context)
    //{
    //  foreach (var kvp in elements)
    //  {
    //    kvp.Value.Graph = this;
    //  }
    //}
  }
}
