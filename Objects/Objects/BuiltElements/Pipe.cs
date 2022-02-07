using System.Collections.Generic;
using Objects.Geometry;
using Objects.Utils;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Objects.BuiltElements
{
  public class Pipe : Base, IDisplayMesh
  {
    public ICurve baseCurve { get; set; }
    public double length { get; set; }
    public double diameter { get; set; }

    [DetachProperty] public Mesh displayMesh { get; set; }

    public string units { get; set; }

    public Pipe() { }

    [SchemaInfo("Pipe", "Creates a Speckle pipe", "BIM", "MEP")]
    public Pipe([SchemaMainParam] ICurve baseCurve, double length, double diameter, double flowrate = 0, double relativeRoughness = 0)
    {
      this.baseCurve = baseCurve;
      this.length = length;
      this.diameter = diameter;
    }

  }
}

namespace Objects.BuiltElements.Revit
{
  public class RevitPipe : Pipe
  {
    public string family { get; set; }
    public string type { get; set; }
    public string systemName { get; set; }
    public string systemType { get; set; }
    public Base parameters { get; set; }
    public string elementId { get; set; }
    public Level level { get; set; }

    public RevitPipe() { }

    [SchemaInfo("RevitPipe", "Creates a Revit pipe", "Revit", "MEP")]
    public RevitPipe(string family, string type, [SchemaMainParam] ICurve baseCurve, double diameter, Level level,  string systemName = "", string systemType = "", List<Parameter> parameters = null)
    {
      this.family = family;
      this.type = type;
      this.baseCurve = baseCurve;
      this.diameter = diameter;
      this.systemName = systemName;
      this.systemType = systemType;
      this.level = level;
      this.parameters = parameters.ToBase();
    }
  }

  public class RevitFlexPipe : RevitPipe
  {
    public Vector startTangent { get; set; }
    public Vector endTangent { get; set; }

    public RevitFlexPipe() { }

    [SchemaInfo("RevitFlexPipe", "Creates a Revit flex pipe", "Revit", "MEP")]
    public RevitFlexPipe(string family, string type, [SchemaMainParam] ICurve baseCurve, double diameter, Level level, Vector startTangent, Vector endTangent, string systemName = "", string systemType = "", List<Parameter> parameters = null)
    {
      this.family = family;
      this.type = type;
      this.baseCurve = baseCurve;
      this.diameter = diameter;
      this.startTangent = startTangent;
      this.endTangent = endTangent;
      this.systemName = systemName;
      this.systemType = systemType;
      this.level = level;
      this.parameters = parameters.ToBase();
    }
  }
}
