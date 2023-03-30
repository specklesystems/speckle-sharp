using System.Collections.Generic;
using Objects.Geometry;
using Objects.Structural.CSI.Analysis;
using Objects.Structural.CSI.Properties;
using Objects.Structural.Geometry;
using Objects.Structural.Materials;
using Objects.Structural.Properties;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using Speckle.Newtonsoft.Json;

namespace Objects.Structural.CSI.Properties
{
  public class CSITendonProperty : Property1D
  {
    public CSITendonProperty()
    {
    }

    public ModelingOption modelingOption { get; set; }
    public double Area { get; set; }


  }
}
