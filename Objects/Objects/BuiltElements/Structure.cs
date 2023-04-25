using System.Collections.Generic;
using Objects.Geometry;
using Speckle.Core.Models;

namespace Objects.BuiltElements;

public class Structure : Base, IDisplayValue<List<Mesh>>
{
  public Point location { get; set; }
  public List<string> pipeIds { get; set; }

  public string units { get; set; }

  [DetachProperty]
  public List<Mesh> displayValue { get; set; }
}
