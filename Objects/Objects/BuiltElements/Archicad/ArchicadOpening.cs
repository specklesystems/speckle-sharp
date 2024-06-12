using System.Collections.Generic;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using Objects.Geometry;

namespace Objects.BuiltElements.Archicad;

public class ArchicadOpening : Opening
{
  [SchemaInfo("ArchicadOpening", "Creates an Archicad opening.", "Archicad", "Structure")]
  public ArchicadOpening() { }

  public string parentApplicationId { get; set; }

  // Element base
  public string? elementType { get; set; } /*APINullabe*/

  public List<Classification>? classifications { get; set; } /*APINullabe*/
  public Base? elementProperties { get; set; }
  public Base? componentProperties { get; set; }

  // Floor Plan Parameters
  public string? floorPlanDisplayMode { get; set; } /*APINullabe*/
  public string? connectionMode { get; set; } /*APINullabe*/

  // Cut Surfaces Parameters
  public bool? cutsurfacesUseLineOfCutElements { get; set; } /*APINullabe*/
  public short? cutsurfacesLinePenIndex { get; set; } /*APINullabe*/
  public string? cutsurfacesLineIndex { get; set; } /*APINullabe*/

  // Outlines Parameters
  public string? outlinesStyle { get; set; } /*APINullabe*/
  public bool? outlinesUseLineOfCutElements { get; set; } /*APINullabe*/
  public string? outlinesUncutLineIndex { get; set; } /*APINullabe*/
  public string? outlinesOverheadLineIndex { get; set; } /*APINullabe*/
  public short? outlinesUncutLinePenIndex { get; set; } /*APINullabe*/
  public short? outlinesOverheadLinePenIndex { get; set; } /*APINullabe*/

  // Opening Cover Fills Parameters
  public bool? useCoverFills { get; set; } /*APINullabe*/
  public bool? useFillsOfCutElements { get; set; } /*APINullabe*/
  public string? coverFillIndex { get; set; } /*APINullabe*/
  public short? coverFillPenIndex { get; set; } /*APINullabe*/
  public short? coverFillBackgroundPenIndex { get; set; } /*APINullabe*/
  public string? coverFillOrientation { get; set; } /*APINullabe*/ // Kérdéses..

  // Cover Fill Transformation Parameters
  public double? coverFillTransformationOrigoX { get; set; }
  public double? coverFillTransformationOrigoY { get; set; }
  public double? coverFillTransformationOrigoZ { get; set; }
  public double? coverFillTransformationXAxisX { get; set; }
  public double? coverFillTransformationXAxisY { get; set; }
  public double? coverFillTransformationXAxisZ { get; set; }
  public double? coverFillTransformationYAxisX { get; set; }
  public double? coverFillTransformationYAxisY { get; set; }
  public double? coverFillTransformationYAxisZ { get; set; }

  // Reference Axis Parameters
  public bool? showReferenceAxis { get; set; } /*APINullabe*/
  public short? referenceAxisPenIndex { get; set; } /*APINullabe*/
  public string? referenceAxisLineTypeIndex { get; set; } /*APINullabe*/
  public double? referenceAxisOverhang { get; set; } /*APINullabe*/

  // Extrusion Geometry Parameters
  // Plane Frame
  public Point extrusionGeometryBasePoint { get; set; }
  public Vector extrusionGeometryXAxis { get; set; }
  public Vector extrusionGeometryYAxis { get; set; }
  public Vector extrusionGeometryZAxis { get; set; }

  // Opening Extrustion Parameters
  public string? basePolygonType { get; set; } /*APINullabe*/
  public double? width { get; set; } /*APINullabe*/
  public double? height { get; set; } /*APINullabe*/
  public string? constraint { get; set; } /*APINullabe*/
  public string? anchor { get; set; } /*APINullabe */
  public int? anchorIndex { get; set; } /*APINullabe*/
  public double? anchorAltitude { get; set; } /*APINullabe*/
  public string? limitType { get; set; } /*APINullabe*/
  public double? extrusionStartOffSet { get; set; } /*APINullabe*/
  public double? finiteBodyLength { get; set; } /*APINullabe*/
  public string? linkedStatus { get; set; } /*APINullabe*/
}
