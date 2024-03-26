using System;
using System.Collections.Generic;
using Objects.Geometry;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using Speckle.Newtonsoft.Json;

namespace Objects.BuiltElements.Archicad;

/*
For further informations about given the variables, visit:
https://archicadapi.graphisoft.com/documentation/api_walltype
*/
public class ArchicadWall : Wall
{
  [SchemaInfo("ArchicadWall", "Creates an Archicad wall.", "Archicad", "Structure")]
  public ArchicadWall() { }

  // Element base
  public string? elementType { get; set; } /*APINullabe*/

  public List<Classification>? classifications { get; set; } /*APINullabe*/
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

  // Wall geometry
  public double? baseOffset { get; set; } /*APINullabe*/
  public Point startPoint { get; set; }
  public Point endPoint { get; set; }

  public string? structure { get; set; } /*APINullabe*/
  public string? geometryMethod { get; set; } /*APINullabe*/
  public string? wallComplexity { get; set; } /*APINullabe*/

  public string? buildingMaterialName { get; set; }
  public string? compositeName { get; set; }
  public string? profileName { get; set; }
  public double? arcAngle { get; set; }

  public ElementShape? shape { get; set; }

  public double? thickness { get; set; } /*APINullabe*/

  public double? outsideSlantAngle { get; set; }
  public double? insideSlantAngle { get; set; }

  public bool? polyWalllCornersCanChange { get; set; }

  // Wall and stories relation
  public double? topOffset { get; set; } /*APINullabe*/
  public short? relativeTopStory { get; set; } /*APINullabe*/
  public string? referenceLineLocation { get; set; } /*APINullabe*/
  public double? referenceLineOffset { get; set; }
  public double? offsetFromOutside { get; set; } /*APINullabe*/
  public int? referenceLineStartIndex { get; set; } /*APINullabe*/
  public int? referenceLineEndIndex { get; set; } /*APINullabe*/
  public bool flipped { get; set; }

  // Floor Plan and Section - Floor Plan Display
  public string? showOnStories { get; set; } /*APINullabe*/
  public string? displayOptionName { get; set; } /*APINullabe*/
  public string? showProjectionName { get; set; } /*APINullabe*/

  // Floor Plan and Section - Cut Surfaces parameters
  public short? cutLinePen { get; set; }
  public string? cutLinetype { get; set; }
  public short? overrideCutFillPen { get; set; }
  public short? overrideCutFillBackgroundPen { get; set; }

  // Floor Plan and Section - Outlines parameters
  public short? uncutLinePen { get; set; } /*APINullabe*/
  public string? uncutLinetype { get; set; } /*APINullabe*/
  public short? overheadLinePen { get; set; } /*APINullabe*/
  public string? overheadLinetype { get; set; } /*APINullabe*/

  // Model - Override Surfaces
  public string? referenceMaterialName { get; set; }
  public int? referenceMaterialStartIndex { get; set; }
  public int? referenceMaterialEndIndex { get; set; }
  public string? oppositeMaterialName { get; set; }
  public int? oppositeMaterialStartIndex { get; set; }
  public int? oppositeMaterialEndIndex { get; set; }
  public string? sideMaterialName { get; set; }
  public bool? materialsChained { get; set; } /*APINullabe*/
  public bool? inheritEndSurface { get; set; } /*APINullabe*/
  public bool? alignTexture { get; set; } /*APINullabe*/
  public int? sequence { get; set; } /*APINullabe*/

  // Model - Log Details (log height, start with half log, surface of horizontal edges, log shape)
  public double? logHeight { get; set; }
  public bool? startWithHalfLog { get; set; }
  public string? surfaceOfHorizontalEdges { get; set; }
  public string? logShape { get; set; }

  // Model - Defines the relation of wall to zones (Zone Boundary, Reduce Zone Area Only, No Effect on Zones)
  public string? wallRelationToZoneName { get; set; } /*APINullabe*/

  // Does it have any embedded object?
  public bool? hasDoor { get; set; } /*APINullabe*/

  public bool? hasWindow { get; set; } /*APINullabe*/
}
