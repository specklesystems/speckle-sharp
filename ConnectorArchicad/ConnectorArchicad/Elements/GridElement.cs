using System.Collections.Generic;
using Objects.Geometry;
using Objects.BuiltElements.Archicad;

namespace Archicad;

public class GridElement
{
  // Speckle-specific properties
  // Base
  public string? id { get; set; }
  public string? applicationId { get; set; }

  // Archicad API properties
  // Element base
  public string? elementType { get; set; }
  public List<Classification>? classifications { get; set; }

  // Grid
  public Point begin { get; set; }
  public Point end { get; set; }
  public string markerText { get; set; }
  public bool isArc { get; set; }
  public double arcAngle { get; set; }

  public GridElement() { }

  public GridElement(string id, string applicationId)
  {
    this.id = id;
    this.applicationId = applicationId;
  }
}
