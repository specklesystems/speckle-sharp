using Speckle.Newtonsoft.Json;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System.Collections.Generic;
using Objects.Geometry;
using Objects.Structural.Geometry;
using Objects.Structural.Properties;
using Objects.Structural.ETABS.Analysis;
using Objects.Structural.Materials;
using Objects.Structural.ETABS.Properties;

namespace Objects.Structural.ETABS.Properties
{
  public class ETABSTendonProperty: Property1D
  {
    public ETABSTendonProperty()
    {
    }

    public ModelingOption modelingOption { get; set; }
    public double Area { get; set; }


  }
}
