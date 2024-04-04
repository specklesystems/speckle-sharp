using System.Collections.Generic;
using Speckle.Core.Models;

namespace Objects.GIS;

public class GisPolygonGeometry : Base
{
  public Base? boundary { get; set; }
  public List<Base>? voids { get; set; }
}
