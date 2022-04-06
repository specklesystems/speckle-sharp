using System;
using System.Collections.Generic;
using System.Text;
using CSiAPIv1;
using Objects.Structural.Geometry;
using Objects.Structural.Analysis;
using Objects.Structural.CSI.Properties;
using System.Linq;

namespace Objects.Converter.CSI
{
  public partial class ConverterCSI
  {
    public Element2D FloorToSpeckle(string name)
    {
      return AreaToSpeckle(name);
    }
  }
}