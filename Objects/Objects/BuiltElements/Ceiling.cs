using System;
using System.Collections.Generic;
using Objects.Geometry;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Objects.BuiltElements
{
  public class Ceiling : Base, IDisplayValue<List<Mesh>>
  {
    public Ceiling() { }

    [SchemaInfo("Ceiling", "Creates a Speckle ceiling", "BIM", "Architecture")]
    public Ceiling(
      [SchemaMainParam] ICurve outline,
      List<ICurve> voids = null,
      [SchemaParamInfo("Any nested elements that this ceiling might have")] List<Base> elements = null
    )
    {
      this.outline = outline;
      this.voids = voids;
      this.elements = elements;
    }

    public ICurve outline { get; set; }
    public List<ICurve> voids { get; set; } = new();

    [DetachProperty]
    public List<Base> elements { get; set; }

    public string units { get; set; }

    [DetachProperty]
    public List<Mesh> displayValue { get; set; }
  }
}

namespace Objects.BuiltElements.Revit
{
  public class RevitCeiling : Ceiling
  {
    public RevitCeiling() { }

    [SchemaDeprecated, SchemaInfo("RevitCeiling", "Creates a Revit ceiling", "Revit", "Architecture")]
    public RevitCeiling(
      [SchemaMainParam, SchemaParamInfo("Planar boundary curve")] ICurve outline,
      string family,
      string type,
      Level level,
      double slope = 0,
      [SchemaParamInfo("Planar line indicating slope direction")] Line slopeDirection = null,
      double offset = 0,
      List<ICurve> voids = null,
      [SchemaParamInfo("Any nested elements that this ceiling might have")] List<Base> elements = null
    )
    {
      this.outline = outline;
      this.family = family;
      this.type = type;
      this.level = level;
      this.slope = slope;
      this.slopeDirection = slopeDirection;
      this.offset = offset;
      this.voids = voids;
      this.elements = elements;
    }

    [SchemaInfo("RevitCeiling", "Creates a Revit ceiling", "Revit", "Architecture")]
    public RevitCeiling(
      [SchemaMainParam, SchemaParamInfo("Planar boundary curve")] ICurve outline,
      string family,
      string type,
      Level level,
      double slope = 0,
      [SchemaParamInfo("Planar line indicating slope direction")] Line slopeDirection = null,
      List<ICurve> voids = null,
      [SchemaParamInfo("Any nested elements that this ceiling might have")] List<Base> elements = null
    )
    {
      this.outline = outline;
      this.family = family;
      this.type = type;
      this.level = level;
      this.slope = slope;
      this.slopeDirection = slopeDirection;
      this.voids = voids;
      this.elements = elements;
    }

    public string family { get; set; }
    public string type { get; set; }
    public Level level { get; set; }
    public double slope { get; set; }
    public Line slopeDirection { get; set; }

    [Obsolete("Offset property is now captured in parameters to match the behavior of other Revit objects")]
    public double offset { get; set; }

    public Base parameters { get; set; }
    public string elementId { get; set; }
  }
}
