using Objects.BuiltElements;
using Objects.Geometry;
using Speckle.Core.Kits;
using System.Collections.Generic;

namespace Objects.Revit
{
  [SchemaVisibility(Visibility.Hidden)]
  public class RevitOpening : RevitElement, IOpening
  {
    public ICurve outline { get; set; }

  }

  [SchemaVisibility(Visibility.Hidden)]
  public class RevitVerticalOpening : RevitOpening
  {
    public Element host { get; set; }

    public int revitHostId { get; set; }

  }

  [SchemaVisibility(Visibility.Hidden)]
  public class RevitWallOpening : RevitOpening
  {
    public RevitWall host { get; set; }

    public int revitHostId { get; set; }

  }

  public class RevitShaft : RevitOpening
  {
    public RevitLevel topLevel { get; set; }
  }
}