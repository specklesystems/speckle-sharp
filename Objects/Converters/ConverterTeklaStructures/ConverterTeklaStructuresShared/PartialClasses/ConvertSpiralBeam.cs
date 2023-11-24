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
using SpiralBeam = Objects.BuiltElements.TeklaStructures.SpiralBeam;
using Point = Objects.Geometry.Point;

namespace Objects.Converter.TeklaStructures
{
  public partial class ConverterTeklaStructures
  {

    public SpiralBeam SpiralBeamToSpeckle(Tekla.Structures.Model.SpiralBeam SpiralBeam)
    {
      var units = GetUnitsFromModel();
      var speckleBeam = new SpiralBeam();
      var curveLine = SpiralBeam.GetCenterLine(false);
      var pointList = new List<double> { };
      foreach (Tekla.Structures.Geometry3d.Point point in curveLine)
      {
        pointList.Add(point.X);
        pointList.Add(point.Y);
        pointList.Add(point.Z);
      }

      speckleBeam.baseLine = new Polyline(pointList,units);

      speckleBeam.profile = GetBeamProfile(SpiralBeam.Profile.ProfileString);
      speckleBeam.material = GetMaterial(SpiralBeam.Material.MaterialString);
      speckleBeam.finish = SpiralBeam.Finish;
      speckleBeam.classNumber = SpiralBeam.Class;
      speckleBeam.name = SpiralBeam.Name;
      var beamCS = SpiralBeam.GetCoordinateSystem();
        speckleBeam.position = GetPositioning(SpiralBeam.Position);
        speckleBeam.alignmentVector = new Objects.Geometry.Vector(beamCS.AxisY.X, beamCS.AxisY.Y, beamCS.AxisY.Z, units);
      GetAllUserProperties(speckleBeam, SpiralBeam);
      speckleBeam.TeklaBeamType = TeklaBeamType.SpiralBeam;
      //var refLine = SpiralBeam.GetReferenceLine(false);
      var solid = SpiralBeam.GetSolid();
      speckleBeam.displayValue = new List<Mesh> { GetMeshFromSolid(solid) };

      speckleBeam.startPoint = new Point(SpiralBeam.StartPoint.X, SpiralBeam.StartPoint.Y, SpiralBeam.StartPoint.Z);
      speckleBeam.rotationAxisPt1 = new Point(SpiralBeam.RotationAxisBasePoint.X, SpiralBeam.RotationAxisBasePoint.Y, SpiralBeam.RotationAxisBasePoint.Z);
      speckleBeam.rotationAxisPt2 = new Point(SpiralBeam.RotationAxisUpPoint.X, SpiralBeam.RotationAxisUpPoint.Y, SpiralBeam.RotationAxisUpPoint.Z);
      speckleBeam.totalRise = SpiralBeam.TotalRise;
      speckleBeam.rotationAngle = SpiralBeam.RotationAngle;
      speckleBeam.twistAngleStart = SpiralBeam.TwistAngleStart;
      speckleBeam.twistAngleEnd = SpiralBeam.TwistAngleEnd;

      var vol = new double();
      var area = new double();
      SpiralBeam.GetReportProperty("VOLUME", ref vol);
      speckleBeam.volume = vol;
      SpiralBeam.GetReportProperty("AREA", ref area);
      speckleBeam.area = area;

      return speckleBeam;
    }
  }
}