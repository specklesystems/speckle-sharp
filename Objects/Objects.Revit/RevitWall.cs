using Objects.Geometry;
using System;
using System.Collections.Generic;
using System.Text;
using Objects.BuiltElements;

namespace Objects.Revit
{
  //[ExposeInSchemaBuilder(false)]
  public class RevitWall : Wall, IRevitElement
  {
    public RevitLevel level { get; set; }
    public string family { get; set; }
    public string type { get; set; }
    public Dictionary<string, object> parameters { get; set; }
    public Dictionary<string, object> typeParameters { get; set; }
    public string elementId { get; set; }
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
