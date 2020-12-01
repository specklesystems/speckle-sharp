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
  }
}

namespace Objects.BuiltElements.Revit
{

  [SchemaIgnore]
  public class RevitOpening : Opening
  {
    [SchemaIgnore]
    public string elementId { get; set; }

    [SchemaOptional]
    public string family { get; set; }

    [SchemaOptional]
    public string type { get; set; }

    [SchemaOptional]
    public Dictionary<string, object> parameters { get; set; }

    [SchemaOptional]
    public Dictionary<string, object> typeParameters { get; set; }
  }

  [SchemaIgnore]
  public class RevitVerticalOpening : RevitOpening
  {
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
    public Level bottomLevel { get; set; }

    public Level topLevel { get; set; }
  }

}
