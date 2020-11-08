using Objects.Geometry;
using System;
using System.Collections.Generic;
using System.Text;
using Objects.BuiltElements;

namespace Objects.Revit
{
  // Specialised class 
  public class RevitWall : Wall, IRevitElement
  {
    public RevitLevel topLevel { get; set; }
    public RevitLevel level { get; set; }
    public new ICurve baseLine { get; set; }

    public string family { get; set; }
    public string type { get; set; }
    public Dictionary<string, object> parameters { get; set; }
    public Dictionary<string, object> typeParameters { get; set; }
    public string elementId { get; set; }
  }

  //[ExposeInheritedMembersInSchemaBuilder(true)]
  public class RevitWallUnconnected : Wall, IRevitElement
  {
    public new double height { get; set; }
    public new ICurve baseLine { get; set; }

    public RevitLevel level { get; set; }

    public string family { get; set; }
    public string type { get; set; }
    public Dictionary<string, object> parameters { get; set; }
    public Dictionary<string, object> typeParameters { get; set; }
    public string elementId { get; set; }
  }

  public class RevitWallByPoint : Element, IRevitElement
  {
    public RevitLevel level { get; set; }
    public Point basePoint { get; set; }

    public string family { get; set; }
    public string type { get; set; }
    public Dictionary<string, object> parameters { get; set; }
    public Dictionary<string, object> typeParameters { get; set; }
    public string elementId { get; set; }
  }

}
