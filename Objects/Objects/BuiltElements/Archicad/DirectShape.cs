using System.Collections.Generic;
using Objects.Geometry;
using Speckle.Core.Models;

namespace Objects.BuiltElements.Archicad;

public class DirectShape : Base
{
  public DirectShape() { }

  public DirectShape(string applicationId, List<Mesh> displayValue)
  {
    this.applicationId = applicationId;
    this.displayValue = displayValue;
  }

  // Element base
  public string elementType { get; set; }
  public List<Classification> classifications { get; set; }

  public ArchicadLevel level { get; set; }

  public string units { get; set; }

  [DetachProperty]
  public List<Mesh> displayValue { get; set; }
}
