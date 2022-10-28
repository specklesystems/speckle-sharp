using Speckle.Core.Models;
using Objects.Geometry;
using System.Collections.Generic;


namespace Objects.BuiltElements.Archicad
{
  public sealed class OpeningBase : Base
  {
    public double width { get; set; }
    public double height { get; set; }
    public double subFloorThickness { get; set; }
    public bool reflected { get; set; }
    public bool oSide { get; set; }
    public bool refSide { get; set; }
  }
  
  public sealed class ArchicadDoor : Base, IDisplayValue<List<Mesh>>
  {

    [DetachProperty]
    public List<Mesh> displayValue { get; set; }
    public OpeningBase openingBase { get; set; }
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
}
