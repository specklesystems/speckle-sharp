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
    public Level topLevel { get; set; }
    public Level bottomLevel { get; set; }
    public new ICurve baseLine { get; set; }

    public Dictionary<string, object> parameters { get; set; } // contains the base offset, etc. that we don't care about; 

    public string family { get; set; }
    public string type { get; set; }
  }

  //[ExposeInheritedMembersInSchemaBuilder(true)]
  public class RevitWallUnconnected : Wall, IRevitElement
  {
    public new double height { get; set; }
    public new ICurve baseLine { get; set; }

    public Level bottomLevel { get; set; }

    public Dictionary<string, object> parameters { get; set; } // contains the base offset, etc. that we don't care about; 

    public string family { get; set; }
    public string type { get; set; }
  }

  public class RevitWallByPoint : Element, IRevitElement
  {
    public Level bottomLevel { get; set; }
    public Point basePoint { get; set; }

    public Dictionary<string, object> parameters { get; set; } // contains the base offset, etc. that we don't care about; 

    public string family { get; set; }
    public string type { get; set; }
  }

}
