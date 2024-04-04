using System.Collections.Generic;
using Speckle.Core.Models;

namespace Objects.GIS;

public class LineElement : Base
{
  public List<ICurve>? geometry { get; set; }
  public Base? attributes { get; set; }
}
