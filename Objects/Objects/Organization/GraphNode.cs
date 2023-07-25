using System;
using Speckle.Core.Models;
using Speckle.Newtonsoft.Json;

namespace Objects.Organization
{
  public class GraphNode : Base
  {
    [Obsolete("This constructor is only for serialization purposes", true)]
    public GraphNode() { }

    public GraphNode(Base @base)
    {
      this.applicationId = @base.applicationId ?? throw new ArgumentException("ApplicationId of base object cannot be null");
      this.Element = @base;
    }

    [JsonIgnore]
    public Graph Graph { get; set; }

    [DetachProperty]
    public Base Element { get; set; }
  }
}
