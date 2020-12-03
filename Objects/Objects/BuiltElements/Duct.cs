using Objects.Geometry;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System.Collections.Generic;

namespace Objects.BuiltElements
{
  public class Duct : Base
  {
    public Line baseLine { get; set; }
    public double width { get; set; }
    public double height { get; set; }
    public double diameter { get; set; }

    public double length { get; set; }
    public double velocity { get; set; }

    public Duct() { }

    [SchemaInfo("Duct", "Creates a Speckle duct")]
    public Duct(Line baseLine, double width, double height, double diameter, double velocity = 0)
    {
      this.baseLine = baseLine;
      this.width = width;
      this.height = height;
      this.diameter = diameter;
      this.velocity = velocity;
    }
  }
}

namespace Objects.BuiltElements.Revit
{

  public class RevitDuct : Duct
  {
    public string type { get; set; }
    public string systemName { get; set; }
    public string systemType { get; set; }
    public Level level { get; set; }
    public Dictionary<string, object> parameters { get; set; }
    public Dictionary<string, object> typeParameters { get; set; }
    public string elementId { get; set; }

    public RevitDuct()
    {
    }

    [SchemaInfo("RevitDuct", "Creates a Revit duct")]
    public RevitDuct(Line baseLine, string type, string systemName, string systemType, Level level, double width, double height, double diameter, double velocity = 0, Dictionary<string, object> parameters = null)
    {
      this.baseLine = baseLine;
      this.type = type;
      this.width = width;
      this.height = height;
      this.diameter = diameter;
      this.velocity = velocity;
      this.systemName = systemName;
      this.systemType = systemType;
      this.parameters = parameters;
      this.level = level;
    }
  }

}
