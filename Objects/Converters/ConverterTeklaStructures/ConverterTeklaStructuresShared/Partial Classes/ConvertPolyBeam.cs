using System;
using System.Collections.Generic;
using Objects.Geometry;
using Objects.Structural.Geometry;
using Objects.Structural.Analysis;
using Speckle.Core.Models;
using BE = Objects.BuiltElements;
using System.Linq;
using Tekla.Structures.Model;
using Tekla.Structures.Solid;
using System.Collections;
using StructuralUtilities.PolygonMesher;

namespace Objects.Converter.TeklaStructures
{
  public partial class ConverterTeklaStructures
  {

    public BE.Beam PolyBeamToSpeckle(PolyBeam PolyBeam)
    {
      var speckleBeam = new BE.Beam();
      var centerPolycurve = PolyBeam.GetCenterLinePolycurve();
      speckleBeam.baseLine = (ICurve)centerPolycurve;
      
      var solid = PolyBeam.GetSolid();
      speckleBeam.displayMesh = GetMeshFromSolid(solid);

  
      return speckleBeam;
    }
  }
}