using System;
using System.Collections.Generic;
using Objects.Geometry;
using Speckle.Core.Models;

namespace Objects.BuiltElements.Archicad;

public class ArchicadFenestration : Base, IDisplayValue<List<Mesh>>
{
  public string parentApplicationId { get; set; }

  public double width { get; set; }
  public double height { get; set; }
  public double subFloorThickness { get; set; }
  public bool reflected { get; set; }
  public bool oSide { get; set; }
  public bool refSide { get; set; }
  public string? verticalLinkTypeName { get; set; }
  public short? verticalLinkStoryIndex { get; set; }
  public bool? wallCutUsing { get; set; }
  public short pen { get; set; }
  public string lineTypeName { get; set; }
  public string buildingMaterial { get; set; }
  public string sectFill { get; set; }
  public short sectFillPen { get; set; }
  public short sectBackgroundPen { get; set; }
  public short sectContPen { get; set; }
  public string cutLineType { get; set; }
  public string aboveViewLineType { get; set; }
  public short aboveViewLinePen { get; set; }
  public short belowViewLinePen { get; set; }
  public string belowViewLineType { get; set; }
  public bool useObjectPens { get; set; }
  public bool useObjLinetypes { get; set; }
  public bool useObjMaterials { get; set; }
  public bool useObjSectAttrs { get; set; }
  public string libraryPart { get; set; }
  public string displayOptionName { get; set; }

  [DetachProperty]
  public List<Mesh> displayValue { get; set; }
}

public class ArchicadDoorWindowBase : ArchicadFenestration
{
  public double revealDepthFromSide { get; set; }
  public double jambDepthHead { get; set; }
  public double jambDepth { get; set; }
  public double jambDepth2 { get; set; }
  public double objLoc { get; set; }
  public double lower { get; set; }
  public string directionType { get; set; }

  public Point startPoint { get; set; }
  public Point dirVector { get; set; }
}

public sealed class ArchicadDoor : ArchicadDoorWindowBase
{
}

public sealed class ArchicadWindow : ArchicadDoorWindowBase
{
}

public sealed class ArchicadSkylight : ArchicadFenestration
{
  public UInt32 vertexID { get; set; }
  public string skylightFixMode { get; set; }
  public string skylightAnchor { get; set; }
  public Point anchorPosition { get; set; }
  public double anchorLevel { get; set; }
  public double azimuthAngle { get; set; }
  public double elevationAngle { get; set; }
}
