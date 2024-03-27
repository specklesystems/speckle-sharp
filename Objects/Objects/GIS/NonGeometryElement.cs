using System.Collections.Generic;
using Speckle.Core.Models;

namespace Objects.GIS;

public class NonGeometryElement : Base
{
  public NonGeometryElement(Base atts)
  {
    attributes = atts;
  }

  public Base? attributes { get; set; }
}
