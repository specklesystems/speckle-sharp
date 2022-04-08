using Speckle.Newtonsoft.Json;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System.Collections.Generic;
using Objects.Geometry;
using Objects.Structural.Geometry;
using Objects.Structural.Loading;
using Objects.Structural.ETABS.Analysis;
using Objects.Structural.Materials;

namespace Objects.Structural.ETABS.Loading
{
    public class ETABSWindLoadingFace : LoadFace
    {
        public double Cp { get; set; }

        public WindPressureType WindPressureType {get;set;}

    public ETABSWindLoadingFace()
    {
    }
  }
}
