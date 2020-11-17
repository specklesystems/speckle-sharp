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
    public bool structural { get; set; }
    public bool flipped { get; set; }
  }

  public class RevitWallByLine : RevitWall
  {
    public RevitLevel topLevel { get; set; }
  }

  public class RevitWallUnconnected : RevitWall
  {
  }

  public class RevitWallByPoint : RevitWall
  {
    public Point basePoint { get; set; }
  }

}
