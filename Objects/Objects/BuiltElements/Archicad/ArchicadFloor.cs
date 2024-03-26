using System;
using System.Collections.Generic;
using Speckle.Core.Models;
using Speckle.Newtonsoft.Json;

namespace Objects.BuiltElements.Archicad;

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
