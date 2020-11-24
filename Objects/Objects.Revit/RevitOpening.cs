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
  public class RevitVerticalOpening : RevitOpening, IHostable
  {
    public Element host { get; set; }

    public int revitHostId { get; set; }

  }

  [SchemaIgnore]
  public class RevitWallOpening : RevitOpening, IHostable
  {
    public RevitWall host { get; set; }

    public int revitHostId { get; set; }

  }

  public class RevitShaft : RevitOpening
  {
    public RevitLevel topLevel { get; set; }
  }
}