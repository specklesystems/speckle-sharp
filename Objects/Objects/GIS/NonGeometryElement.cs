using System;
using Speckle.Core.Models;

namespace Objects.GIS;

[Obsolete("NonGeometryElement was replaced by a more generic class, \"GisFeature\", which contains more information")]
public class NonGeometryElement : Base
{
  public Base? attributes { get; set; }
}
