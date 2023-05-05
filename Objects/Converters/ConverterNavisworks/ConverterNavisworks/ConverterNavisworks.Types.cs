using System.Collections.Generic;
using Objects.Geometry;
using Speckle.Core.Models;

namespace Objects.Core.Models;

internal sealed class GeometryNode : Base
{
  [DetachProperty]
  public List<Base> displayValue { get; set; }
}
