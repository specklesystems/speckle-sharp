using System.Collections.Generic;
using Objects.Geometry;
using Speckle.Core.Models;

namespace Objects.BuiltElements.Archicad
{
  public class DirectShape : Base
  {
    [DetachProperty]
    public List<Mesh> displayValue { get; set; }

    public DirectShape() { }

    public DirectShape(string applicationId, List<Mesh> displayValue)
    {
      this.applicationId = applicationId;
      this.displayValue = displayValue;
    }
  }
}
