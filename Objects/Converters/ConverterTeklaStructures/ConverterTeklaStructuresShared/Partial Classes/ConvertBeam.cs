using System;
using System.Collections.Generic;
using Objects.Geometry;
using Objects.Structural.Geometry;
using Objects.Structural.Analysis;
using Speckle.Core.Models;
using BE = Objects.BuiltElements;

using System.Linq;
using Tekla.Structures.Model;

namespace Objects.Converter.TeklaStructures
{
  public partial class ConverterTeklaStructures
  {

    public BE.Beam BeamToSpeckle(Beam beam)
    {
      var speckleBeam = new BE.Beam();
      return speckleBeam;
    }
  }
}
