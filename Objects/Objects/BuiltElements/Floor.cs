using System;
using System.Collections.Generic;
using Objects.Geometry;
using Objects.Utils;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using Speckle.Newtonsoft.Json;

namespace Objects.BuiltElements
{
  public class Floor : Base, IDisplayValue<List<Mesh>>
  {
    public Floor() { }

    [SchemaInfo("Floor", "Creates a Speckle floor", "BIM", "Architecture")]
    public Floor(
      [SchemaMainParam] ICurve outline,
      List<ICurve>? voids = null,
      [SchemaParamInfo("Any nested elements that this floor might have")] List<Base>? elements = null
    )
    {
      this.outline = outline;

      this.voids = voids ?? new();

      this.elements = elements;
    }

    public ICurve outline { get; set; }
    public List<ICurve> voids { get; set; } = new();

    [DetachProperty]
    public List<Base>? elements { get; set; }
    public virtual Level? level { get; internal set; }
    public string units { get; set; }

    [DetachProperty]
    public List<Mesh> displayValue { get; set; }
  }
}

namespace Objects.BuiltElements.Revit
{
  public class RevitFloor : Floor
  {
    public RevitFloor() { }

    [SchemaInfo("RevitFloor", "Creates a Revit floor by outline and level", "Revit", "Architecture")]
    public RevitFloor(
      string family,
      string type,
      [SchemaMainParam] ICurve outline,
      Level level,
      bool structural = false,
      double slope = 0,
      Line? slopeDirection = null,
      List<ICurve>? voids = null,
      [SchemaParamInfo("Any nested elements that this floor might have")] List<Base>? elements = null,
      List<Parameter>? parameters = null
    )
    {
      this.family = family;
      this.type = type;
      this.level = level;
      this.structural = structural;
      this.slope = slope;
      this.slopeDirection = slopeDirection;
      this.parameters = parameters?.ToBase();
      this.outline = outline;
      this.voids = voids ?? new();
      this.elements = elements;
    }

    public string family { get; set; }
    public string type { get; set; }

    public new Level? level
    {
      get => base.level;
      set => base.level = value;
    }

    public bool structural { get; set; }
    public double slope { get; set; }
    public Line? slopeDirection { get; set; }
    public Base? parameters { get; set; }
    public string elementId { get; set; }
  }
}

namespace Objects.BuiltElements.Archicad
{
  /*
  For further informations about given the variables, visit:
  https://archicadapi.graphisoft.com/documentation/api_slabtype
  */
  public sealed class ArchicadFloor : Floor
  {
    // Element base
    public string? elementType { get; set; } /*APINullable*/

    public List<Classification>? classifications { get; set; } /*APINullable*/
    public Base? elementProperties { get; set; }
    public Base? componentProperties { get; set; }

    public override Level? level
    {
      get => archicadLevel;
      internal set
      {
        if (value is ArchicadLevel or null)
        {
          archicadLevel = value as ArchicadLevel;
        }
        else
        {
          throw new ArgumentException($"Expected object of type {nameof(ArchicadLevel)}");
        }
      }
    }

    [JsonIgnore]
    public ArchicadLevel? archicadLevel { get; set; } /*APINullabe*/

    public string? layer { get; set; } /*APINullabe*/

    // Geometry and positioning
    public double? thickness { get; set; }
    public ElementShape shape { get; set; }
    public string? structure { get; set; } /*APINullabe*/
    public string? compositeName { get; set; }
    public string? buildingMaterialName { get; set; }
    public string? referencePlaneLocation { get; set; } /*APINullabe*/

    // EdgeTrims
    public string? edgeAngleType { get; set; }
    public double? edgeAngle { get; set; }

    // Floor Plan and Section - Floor Plan Display
    public string? showOnStories { get; set; } /*APINullabe*/
    public Visibility? visibilityCont { get; set; }
    public Visibility? visibilityFill { get; set; }

    // Floor Plan and Section - Cut Surfaces
    public short? sectContPen { get; set; }
    public string? sectContLtype { get; set; }
    public short? cutFillPen { get; set; }
    public short? cutFillBackgroundPen { get; set; }

    // Floor Plan and Section - Outlines
    public short? contourPen { get; set; }
    public string? contourLineType { get; set; }
    public short? hiddenContourLinePen { get; set; }
    public string? hiddenContourLineType { get; set; }

    // Floor Plan and Section - Cover Fills
    public bool? useFloorFill { get; set; }
    public short? floorFillPen { get; set; }
    public short? floorFillBGPen { get; set; }
    public string? floorFillName { get; set; }
    public bool? use3DHatching { get; set; }
    public string? hatchOrientation { get; set; }
    public double? hatchOrientationOrigoX { get; set; }
    public double? hatchOrientationOrigoY { get; set; }
    public double? hatchOrientationXAxisX { get; set; }
    public double? hatchOrientationXAxisY { get; set; }
    public double? hatchOrientationYAxisX { get; set; }
    public double? hatchOrientationYAxisY { get; set; }

    // Model
    public string? topMat { get; set; }
    public string? sideMat { get; set; }
    public string? botMat { get; set; }
    public bool? materialsChained { get; set; }

    public class Visibility : Base
    {
      public bool? showOnHome { get; set; }
      public bool? showAllAbove { get; set; }
      public bool? showAllBelow { get; set; }
      public short? showRelAbove { get; set; }
      public short? showRelBelow { get; set; }
    }
  }
}
