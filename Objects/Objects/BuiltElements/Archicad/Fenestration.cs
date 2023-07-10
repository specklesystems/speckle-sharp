using System;
using System.Collections.Generic;
using Objects.Geometry;
using Speckle.Core.Models;

namespace Objects.BuiltElements.Archicad;

public class ArchicadFenestration : Base, IDisplayValue<List<Mesh>>
{
  public string parentApplicationId { get; set; }

  // Element base
  public string? /*APINullabe*/ elementType { get; set; }
  public List<Classification>? /*APINullabe*/ classifications { get; set; }

  public double? /*APINullabe*/ width { get; set; }
  public double? /*APINullabe*/ height { get; set; }
  public double? /*APINullabe*/ subFloorThickness { get; set; }
  public bool? /*APINullabe*/ reflected { get; set; }
  public bool? /*APINullabe*/ oSide { get; set; }
  public bool? /*APINullabe*/ refSide { get; set; }
  public string? verticalLinkTypeName { get; set; }
  public short? verticalLinkStoryIndex { get; set; }
  public bool? wallCutUsing { get; set; }
  public short? /*APINullabe*/ pen { get; set; }
  public string? /*APINullabe*/ lineTypeName { get; set; }
  public string? /*APINullabe*/ buildingMaterial { get; set; }
  public string? /*APINullabe*/ sectFill { get; set; }
  public short? /*APINullabe*/ sectFillPen { get; set; }
  public short? /*APINullabe*/ sectBackgroundPen { get; set; }
  public short? /*APINullabe*/ sectContPen { get; set; }
  public string? /*APINullabe*/ cutLineType { get; set; }
  public string? /*APINullabe*/ aboveViewLineType { get; set; }
  public short? /*APINullabe*/ aboveViewLinePen { get; set; }
  public short? /*APINullabe*/ belowViewLinePen { get; set; }
  public string? /*APINullabe*/ belowViewLineType { get; set; }
  public bool? /*APINullabe*/ useObjectPens { get; set; }
  public bool? /*APINullabe*/ useObjLinetypes { get; set; }
  public bool? /*APINullabe*/ useObjMaterials { get; set; }
  public bool? /*APINullabe*/ useObjSectAttrs { get; set; }
  public string? /*APINullabe*/ libraryPart { get; set; }
  public string? /*APINullabe*/ displayOptionName { get; set; }

  [DetachProperty]
  public List<Mesh> displayValue { get; set; }
}

public class ArchicadDoorWindowBase : ArchicadFenestration
{
  public double? /*APINullabe*/ revealDepthFromSide { get; set; }
  public double? /*APINullabe*/ jambDepthHead { get; set; }
  public double? /*APINullabe*/ jambDepth { get; set; }
  public double? /*APINullabe*/ jambDepth2 { get; set; }
  public double? /*APINullabe*/ objLoc { get; set; }
  public double? /*APINullabe*/ lower { get; set; }
  public string? /*APINullabe*/ directionType { get; set; }

  public Point? /*APINullabe*/ startPoint { get; set; }
  public Point? /*APINullabe*/ dirVector { get; set; }
}

public sealed class ArchicadDoor : ArchicadDoorWindowBase
{
}

public sealed class ArchicadWindow : ArchicadDoorWindowBase
{
}

public sealed class ArchicadSkylight : ArchicadFenestration
{
  public UInt32? /*APINullabe*/ vertexID { get; set; }
  public string? /*APINullabe*/ skylightFixMode { get; set; }
  public string? /*APINullabe*/ skylightAnchor { get; set; }
  public Point? /*APINullabe*/ anchorPosition { get; set; }
  public double? /*APINullabe*/ anchorLevel { get; set; }
  public double? /*APINullabe*/ azimuthAngle { get; set; }
  public double? /*APINullabe*/ elevationAngle { get; set; }
}
