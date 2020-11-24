using Objects.Geometry;
using System;
using System.Collections.Generic;
using System.Text;
using Objects.BuiltElements;
using Speckle.Core.Kits;

namespace Objects.Revit
{
  [SchemaIgnore]
  public class RevitWall : RevitElement, IWall
  {
    public double height { get; set; }
    public ICurve baseLine { get; set; }

    [SchemaOptional]
    public bool structural { get; set; }

    [SchemaOptional]
    public bool flipped { get; set; }
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
