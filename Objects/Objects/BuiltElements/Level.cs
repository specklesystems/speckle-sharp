using Speckle.Core.Kits;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Objects.BuiltElements
{
  public class Level : Base
  {
    public string name { get; set; }

    [SchemaDescription("Leave blank if planning to reuse an existing level in your Revit document.")]
    [SchemaOptional]
    public double? elevation { get; set; }

    [SchemaDescription("Set in here any nested elements that this level might have.")]
    [SchemaOptional]
    public List<Base> elements { get; set; }

    public Level() { }

    public Level(string name, double elevation)
    {
      this.name = name;
      this.elevation = elevation;
    }
  }
}

namespace Objects.BuiltElements.Revit
{

  public class RevitLevel : Level
  {
    [SchemaOptional]
    [SchemaDescription("If true, it creates an associated view in Revit")]
    public bool createView { get; set; }

    [SchemaOptional]
    public Dictionary<string, object> parameters { get; set; }

    [SchemaIgnore]
    public string elementId { get; set; }
  }
}
