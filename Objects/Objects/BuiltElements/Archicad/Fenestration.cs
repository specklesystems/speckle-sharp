using System.Collections.Generic;
using Objects.Geometry;
using Speckle.Core.Models;

namespace Objects.BuiltElements.Archicad;

public class ArchicadFenestration : Base, IDisplayValue<List<Mesh>>
{
  public string parentApplicationId { get; set; }

  // Element base
  public string? elementType { get; set; } /*APINullabe*/

  public List<Classification>? classifications { get; set; } /*APINullabe*/
  public Base? elementProperties { get; set; }
  public Base? componentProperties { get; set; }

  public double? width { get; set; } /*APINullabe*/
  public double? height { get; set; } /*APINullabe*/
  public double? subFloorThickness { get; set; } /*APINullabe*/
  public bool? reflected { get; set; } /*APINullabe*/
  public bool? oSide { get; set; } /*APINullabe*/
  public bool? refSide { get; set; } /*APINullabe*/
  public string? verticalLinkTypeName { get; set; }
  public short? verticalLinkStoryIndex { get; set; }
  public bool? wallCutUsing { get; set; }
  public short? pen { get; set; } /*APINullabe*/
  public string? lineTypeName { get; set; } /*APINullabe*/
  public string? buildingMaterial { get; set; } /*APINullabe*/
  public string? sectFill { get; set; } /*APINullabe*/
  public short? sectFillPen { get; set; } /*APINullabe*/
  public short? sectBackgroundPen { get; set; } /*APINullabe*/
  public short? sectContPen { get; set; } /*APINullabe*/
  public string? cutLineType { get; set; } /*APINullabe*/
  public string? aboveViewLineType { get; set; } /*APINullabe*/
  public short? aboveViewLinePen { get; set; } /*APINullabe*/
  public short? belowViewLinePen { get; set; } /*APINullabe*/
  public string? belowViewLineType { get; set; } /*APINullabe*/
  public bool? useObjectPens { get; set; } /*APINullabe*/
  public bool? useObjLinetypes { get; set; } /*APINullabe*/
  public bool? useObjMaterials { get; set; } /*APINullabe*/
  public bool? useObjSectAttrs { get; set; } /*APINullabe*/
  public string? libraryPart { get; set; } /*APINullabe*/
  public string? displayOptionName { get; set; } /*APINullabe*/

  [DetachProperty]
  public List<Mesh> displayValue { get; set; }
}

public class ArchicadDoorWindowBase : ArchicadFenestration
{
  public double? revealDepthFromSide { get; set; } /*APINullabe*/
  public double? jambDepthHead { get; set; } /*APINullabe*/
  public double? jambDepth { get; set; } /*APINullabe*/
  public double? jambDepth2 { get; set; } /*APINullabe*/
  public double? objLoc { get; set; } /*APINullabe*/
  public double? lower { get; set; } /*APINullabe*/
  public string? directionType { get; set; } /*APINullabe*/

  public Point? startPoint { get; set; } /*APINullabe*/
  public Point? dirVector { get; set; } /*APINullabe*/
}

public sealed class ArchicadDoor : ArchicadDoorWindowBase { }

public sealed class ArchicadWindow : ArchicadDoorWindowBase { }

public sealed class ArchicadSkylight : ArchicadFenestration
{
  public uint? vertexID { get; set; } /*APINullabe*/
  public string? skylightFixMode { get; set; } /*APINullabe*/
  public string? skylightAnchor { get; set; } /*APINullabe*/
  public Point? anchorPosition { get; set; } /*APINullabe*/
  public double? anchorLevel { get; set; } /*APINullabe*/
  public double? azimuthAngle { get; set; } /*APINullabe*/
  public double? elevationAngle { get; set; } /*APINullabe*/
}
