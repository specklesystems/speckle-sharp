using Speckle.Newtonsoft.Json;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System.Collections.Generic;
using Objects.Geometry;
using Objects.Structural.Geometry;
using Objects.Structural.Properties;
using Objects.Structural.ETABS.Analysis;
using Objects.Structural.Materials;

namespace Objects.Structural.ETABS.Geometry
{
  public class ETABSElement2D : Element2D
  {
    public double[] modifiers { get; set; } 

  }
}
