using Objects.Geometry;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System.Collections.Generic;

namespace Objects.Revit
{
  [SchemaIgnore]
  public class RevitWall : Base, IRevitHasFamilyAndType, IRevitHasParameters, IRevitHasTypeParameters, IWall
  {
    public double height { get; set; }

    public ICurve baseLine { get; set; }

    [SchemaOptional]
    public bool structural { get; set; }

    [SchemaOptional]
    public bool flipped { get; set; }

    [SchemaOptional]
    public string family { get; set; }

    [SchemaOptional]
    public string type { get; set; }

    [SchemaOptional]
    public Dictionary<string, object> parameters { get; set; }

    [SchemaOptional]
    public Dictionary<string, object> typeParameters { get; set; }

    [SchemaIgnore]
    public string elementId { get; set; }

    [SchemaOptional]
    public ILevel level { get; set; }
  }

  [SchemaDescription("A Revit wall by base line and top and bottom levels")]
  public class RevitWallByLine : RevitWall
  {
    public RevitLevel topLevel { get; set; }

    //hiding the height as not needed here
    [SchemaIgnore]
    public double height { get; set; }
  }

  [SchemaDescription("A Revit wall by base line, bottom level and height")]
  public class RevitWallUnconnected : RevitWall
  {
  }

  [SchemaDescription("A Revit wall by point and family")]
  public class RevitWallByPoint : RevitWall
  {
    public Point basePoint { get; set; }
  }

}
