using Newtonsoft.Json;
using Objects.Geometry;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System.Collections.Generic;

namespace Objects.Revit
{
  public class RevitFamilyInstance : Base, IRevitHasFamilyAndType
  {
    
    public Point basePoint { get; set; }

    public string family { get; set; }

    public string type { get; set; }

    [SchemaOptional]
    public bool facingFlipped { get; set; }

    [SchemaOptional]
    public bool handFlipped { get; set; }

    [SchemaOptional]
    public double rotation { get; set; }

    [JsonIgnore]
    [SchemaIgnore]
    public string revitHostId { get; set; }

    [SchemaOptional]
    public Dictionary<string, object> parameters { get; set; }
    
    [SchemaOptional]
    public Dictionary<string, object> typeParameters { get; set; }

    [SchemaOptional]
    public ILevel level { get; set; }

    [SchemaIgnore]
    public string elementId { get; set; }

  }
}