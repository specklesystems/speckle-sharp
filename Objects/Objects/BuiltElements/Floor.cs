using System;
using Objects.Geometry;
using Objects.Utils;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System.Collections.Generic;
using System.Linq;
using Speckle.Newtonsoft.Json;

namespace Objects.BuiltElements
{
  public class Floor : Base, IDisplayValue<List<Mesh>>
  {
    public ICurve outline { get; set; }
    public List<ICurve> voids { get; set; } = new List<ICurve>();

    [DetachProperty]
    public List<Base> elements { get; set; }
    
    [DetachProperty]
    public List<Mesh> displayValue { get; set; }

    public string units { get; set; }

    public Floor() { }

    [SchemaInfo("Floor", "Creates a Speckle floor", "BIM", "Architecture")]
    public Floor([SchemaMainParam] ICurve outline, List<ICurve> voids = null,
      [SchemaParamInfo("Any nested elements that this floor might have")] List<Base> elements = null)
    {
      this.outline = outline;
      this.voids = voids;
      this.elements = elements;
    }
  }
}

namespace Objects.BuiltElements.Revit
{
  public class RevitFloor : Floor
  {
    public string family { get; set; }
    public string type { get; set; }
    public Level level { get; set; }
    public bool structural { get; set; }
    public double slope { get; set; }
    public Line slopeDirection { get; set; }
    public Base parameters { get; set; }
    public string elementId { get; set; }
    public RevitFloor() { }

    [SchemaInfo("RevitFloor", "Creates a Revit floor by outline and level", "Revit", "Architecture")]
    public RevitFloor(string family, string type, [SchemaMainParam] ICurve outline,
       Level level, bool structural = false, double slope = 0, Line slopeDirection = null, List<ICurve> voids = null,
      [SchemaParamInfo("Any nested elements that this floor might have")] List<Base> elements = null,
      List<Parameter> parameters = null)
    {
      this.family = family;
      this.type = type;
      this.level = level;
      this.structural = structural;
      this.slope = slope;
      this.slopeDirection = slopeDirection;
      this.parameters = parameters.ToBase();
      this.outline = outline;
      this.voids = voids;
      this.elements = elements;
    }
  }
}


namespace Objects.BuiltElements.Archicad
{
  public sealed class ArchicadFloor : BuiltElements.Floor
  {
    public ElementShape shape { get; set; }

    public int? floorIndex { get; set; }

    public string structure { get; set; }

    public double? thickness { get; set; }

    public string edgeAngleType { get; set; }

    public double? edgeAngle { get; set; }

    public string referencePlaneLocation { get; set; }

    public int? compositeIndex { get; set; }

    public int? buildingMaterialIndex { get; set; }

    public ArchicadFloor() { }
  }
}