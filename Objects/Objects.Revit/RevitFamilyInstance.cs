using Objects.BuiltElements;
using Objects.Geometry;
using Speckle.Core.Kits;
using System.Collections.Generic;

namespace Objects.Revit
{
  public class RevitFamilyInstance : RevitElement
  {
    public Element host { get; set; }
    public Point basePoint { get; set; }

    [SchemaIgnore]
    public int revitHostId { get; set; }
    public bool facingFlipped { get; set; }
    public bool handFlipped { get; set; }
    public double rotation { get; set; }
  }
}