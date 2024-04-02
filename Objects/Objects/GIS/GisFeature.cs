using System.Collections.Generic;
using Speckle.Core.Models;

namespace Objects.GIS;

public class GisFeature : Base
{
  [DetachProperty]
  public List<Base>? geometry { get; set; }

  [DetachProperty]
  public List<Base>? displayValue { get; set; }
  public Base? attributes { get; set; }
}
