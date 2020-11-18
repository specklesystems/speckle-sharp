using Objects.BuiltElements;
using Objects.Geometry;
using Speckle.Core.Kits;
using System.Collections.Generic;

namespace Objects.Revit
{
  [SchemaIgnore]
  public class RevitOpening : RevitElement, IOpening
  {
    public ICurve outline { get; set; }

  }

  [SchemaIgnore]
  public class RevitVerticalOpening : RevitOpening
  {
    public Element host { get; set; }

    public int revitHostId { get; set; }

  }

  [SchemaIgnore]
  public class RevitWallOpening : RevitOpening
  {
    public RevitWall host { get; set; }

    public int revitHostId { get; set; }

  }

  public class RevitShaft : RevitOpening
  {
    public string topLevel { get; set; }
  }
}