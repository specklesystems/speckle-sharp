using Objects.Geometry;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Objects.BuiltElements.Revit
{
  public class StructuralConnectionHandler : Base, IDisplayValue<List<Mesh>>
  {
    public string family { get; set; }
    public string type { get; set; }
    public Point basePoint { get; set; }
    [DetachProperty]
    public List<Mesh> displayValue { get; set; }
    [DetachProperty]
    public List<Base> connectedElements { get; set; }
    public StructuralConnectionHandler() { }
  }
}
