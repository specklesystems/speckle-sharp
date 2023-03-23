using System;
using System.Collections.Generic;
using System.Text;
using Objects.Structural.Geometry;
using Objects.Structural.CSI.Properties;
using Speckle.Core.Kits;
using Objects.Structural.Properties;
using Objects.Geometry;
using Speckle.Core.Models;

namespace Objects.Structural.CSI.Geometry
{
  public class CSITendon : CSIElement1D
  {
    public Polycurve polycurve { get; set; }

    [DetachProperty]
    public CSITendonProperty CSITendonProperty { get; set; }


    public CSITendon(string name, Polycurve polycurve, CSITendonProperty CSITendonProperty)
    {
      this.name = name;
      this.polycurve = polycurve;
      CSITendonProperty = CSITendonProperty;
    }

    public CSITendon()
    {
    }
  }
}
