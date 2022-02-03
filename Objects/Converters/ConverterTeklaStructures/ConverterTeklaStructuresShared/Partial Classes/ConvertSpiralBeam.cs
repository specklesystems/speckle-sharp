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
using Tekla.Structures.Geometry3d;

namespace Objects.Converter.TeklaStructures
{
  public partial class ConverterTeklaStructures
  {

    public BE.Beam SpiralBeamToSpeckle(SpiralBeam SpiralBeam)
    {
      var units = GetUnitsFromModel();
      var speckleBeam = new BE.Beam();
      var curveLine = SpiralBeam.GetCenterLine(false);
      var pointList = new List<double> { };
      foreach (Tekla.Structures.Geometry3d.Point point in curveLine)
      {
        pointList.Add(point.X);
        pointList.Add(point.Y);
        pointList.Add(point.Z);
      }
      speckleBeam.baseLine = new Polyline(pointList,units);
      //var refLine = SpiralBeam.GetReferenceLine(false);
      var solid = SpiralBeam.GetSolid();
      speckleBeam.displayMesh = GetMeshFromSolid(solid);


      return speckleBeam;
    }
  }
}