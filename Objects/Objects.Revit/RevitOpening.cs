using Objects.BuiltElements;
using Objects.Geometry;
using Speckle.Core.Kits;
using System.Collections.Generic;

namespace Objects.Revit
{
  [SchemaBuilderIgnore]
  public class RevitVerticalOpening : RevitElement, IOpening
  {
    public Element host { get; set; }

    public int revitHostId { get; set; }
    public ICurve outline { get; set; }

  }

  [SchemaBuilderIgnore]
  public class RevitWallOpening : RevitElement, IOpening
  {
    public RevitWall host { get; set; }

    public int revitHostId { get; set; }
    public ICurve outline { get; set; }

  }

  public class RevitShaft : RevitElement, IOpening
  {
    public RevitLevel topLevel { get; set; }
    public ICurve outline { get; set; }
  }
}