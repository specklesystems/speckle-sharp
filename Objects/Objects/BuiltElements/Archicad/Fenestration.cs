using Objects.Geometry;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;

namespace Objects.BuiltElements.Archicad
{
  public class ArchicadFenestration : Base, IDisplayValue<List<Mesh>>
    {
    [DetachProperty]
    public List<Mesh> displayValue { get; set; }

    public string parentApplicationId { get; set; }

    public double width { get; set; }
    public double height { get; set; }
    public double subFloorThickness { get; set; }
    public bool reflected { get; set; }
    public bool oSide { get; set; }
    public bool refSide { get; set; }

    public string buildingMaterial { get; set; }
    public string libraryPart { get; set; }

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

  public sealed class ArchicadDoor : ArchicadFenestration
  {
  }

  public sealed class ArchicadWindow : ArchicadFenestration
  {
  }
}