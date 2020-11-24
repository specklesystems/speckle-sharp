using Newtonsoft.Json;
using Objects.BuiltElements;
using Objects.Geometry;
using Speckle.Core.Kits;
using System.Collections.Generic;

namespace Objects.Revit
{
  public class RevitFamilyInstance : RevitElement, IHostable
  {
    public Element host { get; set; }

    public Point basePoint { get; set; }

    [SchemaOptional]
    public bool facingFlipped { get; set; }

    [SchemaOptional]
    public bool handFlipped { get; set; }

    [SchemaOptional]
    public double rotation { get; set; }

    [JsonIgnore]
    [SchemaIgnore]
    public int revitHostId { get; set; }
  }
}