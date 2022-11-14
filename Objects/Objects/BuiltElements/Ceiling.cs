﻿using System;
using Objects.Geometry;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System.Collections.Generic;
using System.Linq;
using Speckle.Newtonsoft.Json;

namespace Objects.BuiltElements
{
  public class Ceiling : Base, IDisplayValue<List<Mesh>>
  {
    public ICurve outline { get; set; }
    public List<ICurve> voids { get; set; } = new List<ICurve>();
    
    [DetachProperty]
    public List<Mesh> displayValue { get; set; }

    [DetachProperty]
    public List<Base> elements { get; set; }

    public string units { get; set; }

    public Ceiling() { }

    [SchemaInfo("Ceiling", "Creates a Speckle ceiling", "BIM", "Architecture")]
    public Ceiling([SchemaMainParam] ICurve outline, List<ICurve> voids = null,
      [SchemaParamInfo("Any nested elements that this ceiling might have")] List<Base> elements = null)
    {
      this.outline = outline;
      this.voids = voids;
      this.elements = elements;
    }
  }
}

namespace Objects.BuiltElements.Revit
{
  public class RevitCeiling : Ceiling
  {
    public string family { get; set; }
    public string type { get; set; }
    public Level level { get; set; }
    public double slope { get; set; }
    public Line slopeDirection { get; set; }
    public double offset { get; set; }
    public Base parameters { get; set; }
    public string elementId { get; set; }

    public RevitCeiling() { }

    [SchemaInfo("RevitCeiling", "Creates a Revit ceiling", "Revit", "Architecture")]
    public RevitCeiling([SchemaMainParam][SchemaParamInfo("Planar boundary curve")] ICurve outline, string family, string type, Level level, 
      double slope = 0, [SchemaParamInfo("Planar line indicating slope direction")] Line slopeDirection = null, double offset = 0, 
      List<ICurve> voids = null, [SchemaParamInfo("Any nested elements that this ceiling might have")] List<Base> elements = null)
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
  }
}