using System;
using System.Collections.Generic;
using System.Text;
using Objects.Structural.Geometry;
using Objects.Structural.ETABS.Properties;
using Speckle.Core.Kits;
using Objects.Structural.Properties;
using Objects.Geometry;

namespace Objects.Structural.ETABS.Geometry
{
  public class ETABSTendon : ETABSElement1D
  {
    public Polycurve polycurve { get; set; }

    public ETABSTendonProperty ETABSTendonProperty { get; set; }

    public ETABSTendon(string name,Polycurve polycurve, ETABSTendonProperty eTABSTendonProperty)
    {
      this.name = name;
      this.polycurve = polycurve;
      ETABSTendonProperty = eTABSTendonProperty;
    }

    public ETABSTendon()
    {
    }
  }
}
