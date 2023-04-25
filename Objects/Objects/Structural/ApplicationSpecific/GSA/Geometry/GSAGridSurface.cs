using System.Collections.Generic;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Objects.Structural.GSA.Geometry;

public class GSAGridSurface : Base
{
  public GSAGridSurface() { }

  [SchemaInfo("GSAGridSurface", "Creates a Speckle structural grid surface for GSA", "GSA", "Geometry")]
  public GSAGridSurface(
    string name,
    int nativeId,
    GSAGridPlane gridPlane,
    double tolerance,
    double spanDirection,
    LoadExpansion loadExpansion,
    GridSurfaceSpanType span,
    List<Base> elements
  )
  {
    this.name = name;
    this.nativeId = nativeId;
    this.gridPlane = gridPlane;
    this.tolerance = tolerance;
    this.spanDirection = spanDirection;
    this.loadExpansion = loadExpansion;
    this.span = span;
    this.elements = elements;
  }

  public string name { get; set; }
  public int nativeId { get; set; }

  [DetachProperty]
  public GSAGridPlane gridPlane { get; set; }

  public double tolerance { get; set; }
  public double spanDirection { get; set; }
  public LoadExpansion loadExpansion { get; set; }
  public GridSurfaceSpanType span { get; set; }

  [DetachProperty, Chunkable(5000)]
  public List<Base> elements { get; set; }
}

public enum GridSurfaceSpanType
{
  NotSet = 0,
  OneWay,
  TwoWay
}

public enum LoadExpansion
{
  NotSet = 0,
  Legacy = 1,
  PlaneAspect = 2,
  PlaneSmooth = 3,
  PlaneCorner = 4
}
