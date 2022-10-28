using Speckle.Core.Models;
using Objects.Geometry;
using System.Collections.Generic;


namespace Objects.BuiltElements.Archicad
{
  public sealed class ArchicadWindow : Base, IDisplayValue<List<Mesh>>
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
