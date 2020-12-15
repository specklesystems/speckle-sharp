using Objects.Geometry;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Objects.BuiltElements
{
  public class Opening : Base
  {
    public ICurve outline { get; set; }

    public Opening() { }

    //[SchemaInfo("Opening", "Creates a Speckle opening")]
    public Opening(ICurve outline)
    {
      this.outline = outline;
    }
  }
}

namespace Objects.BuiltElements.Revit
{

  public class RevitOpening : Opening
  {
    //public string family { get; set; }

    //public string type { get; set; }

    public List<Parameter> parameters { get; set; }

    public string elementId { get; set; }

    public RevitOpening() { }
  }

  public class RevitVerticalOpening : RevitOpening
  {
  }

  public class RevitWallOpening : RevitOpening
  {
    public RevitWall host { get; set; }

    public RevitWallOpening() { }
  }

  public class RevitShaft : RevitOpening
  {
    public Level bottomLevel { get; set; }

    public Level topLevel { get; set; }

    public double height { get; set; }

    public RevitShaft() { }

    [SchemaInfo("RevitShaft", "Creates a Revit shaft")]
    public RevitShaft(ICurve outline, Level bottomLevel, Level topLevel, List<Parameter> parameters = null)
    {
      this.outline = outline;
      this.bottomLevel = bottomLevel;
      this.topLevel = topLevel;
      this.parameters = parameters;
    }
  }

}
