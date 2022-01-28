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
      //var centerPolycurve = PolyBeam.GetCenterLinePolycurve();
      var polyLine = new Polycurve();
      //foreach (var curve in centerPolycurve)
      //{
      //  var startPt = new Point(curve.StartPoint.X, curve.StartPoint.Y, curve.StartPoint.Z);
      //  var endPt = new Point(curve.EndPoint.X, curve.EndPoint.Y, curve.EndPoint.Z);
      //  var line = new Line(startPt, endPt);
      //  polyLine.segments.Add(line);
        
      //}
      speckleBeam.baseLine = polyLine;
      var solid = PolyBeam.GetSolid();
      speckleBeam.displayMesh = GetMeshFromSolid(solid);


      return speckleBeam;
    }
  }
}