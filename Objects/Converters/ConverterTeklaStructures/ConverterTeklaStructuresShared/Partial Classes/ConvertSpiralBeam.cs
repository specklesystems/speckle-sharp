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
using Objects.BuiltElements.TeklaStructures;

namespace Objects.Converter.TeklaStructures
{
  public partial class ConverterTeklaStructures
  {

    public TeklaBeam SpiralBeamToSpeckle(SpiralBeam SpiralBeam)
    {
      var units = GetUnitsFromModel();
      var speckleBeam = new TeklaBeam();
      var curveLine = SpiralBeam.GetCenterLine(false);
      var pointList = new List<double> { };
      foreach (Tekla.Structures.Geometry3d.Point point in curveLine)
      {
        pointList.Add(point.X);
        pointList.Add(point.Y);
        pointList.Add(point.Z);
      }

      speckleBeam.baseLine = new Polyline(pointList,units);
      speckleBeam.profile = GetProfile(SpiralBeam.Profile.ProfileString);
      speckleBeam.material = GetMaterial(SpiralBeam.Material.MaterialString);
      speckleBeam.finish = SpiralBeam.Finish;
      speckleBeam.classNumber = SpiralBeam.Class;
      speckleBeam.name = SpiralBeam.Name;
      var beamCS = SpiralBeam.GetCoordinateSystem();
      speckleBeam.alignmentVector = new Objects.Geometry.Vector(beamCS.AxisY.X, beamCS.AxisY.Y, beamCS.AxisY.Z, units);
      GetAllUserProperties(speckleBeam, SpiralBeam);
      speckleBeam.TeklaBeamType = TeklaBeamType.SpiralBeam;
      //var refLine = SpiralBeam.GetReferenceLine(false);
      var solid = SpiralBeam.GetSolid();
      speckleBeam.displayMesh = GetMeshFromSolid(solid);


      return speckleBeam;
    }
  }
}