using Objects.BuiltElements;
using Objects.Geometry;
using System.Collections.Generic;

namespace Objects.Revit
{
  public class RevitOpening : Opening, IRevitElement
  {
    public string family { get; set; }
    public string type { get; set; }
    public Dictionary<string, object> parameters { get; set; }
    public Dictionary<string, object> typeParameters { get; set; }
    public string elementId { get; set; }
    public RevitLevel level { get; set; }
  }

  public class RevitVerticalOpening : RevitOpening
  {
    public new string family { get; set; }
    public new string type { get; set; }
    public new Dictionary<string, object> parameters { get; set; }
    public new Dictionary<string, object> typeParameters { get; set; }
    public new string elementId { get; set; }
    public new RevitLevel level { get; set; }
    public Element host { get; set; }

    public int revitHostId { get; set; }

    public new ICurve outline { get; set; }
  }

  public class RevitWallOpening : RevitOpening
  {
    public RevitWall host { get; set; }

    public new RevitLevel level { get; set; }

    public new string family { get; set; }
    public new string type { get; set; }
    public new Dictionary<string, object> parameters { get; set; }
    public new Dictionary<string, object> typeParameters { get; set; }
    public new string elementId { get; set; }

    public int revitHostId { get; set; }

    public new Polyline outline { get; set; }
  }

  public class RevitShaft : RevitOpening
  {
    public RevitLevel topLevel { get; set; }
    public new RevitLevel level { get; set; }

    public new string family { get; set; }
    public new string type { get; set; }
    public new Dictionary<string, object> parameters { get; set; }
    public new Dictionary<string, object> typeParameters { get; set; }
    public new string elementId { get; set; }

    public new ICurve outline { get; set; }
  }
}