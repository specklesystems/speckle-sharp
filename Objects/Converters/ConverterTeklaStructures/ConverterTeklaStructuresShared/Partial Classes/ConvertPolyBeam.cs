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
using Objects.BuiltElements.TeklaStructures;

namespace Objects.Converter.TeklaStructures
{
  public partial class ConverterTeklaStructures
  {

    public TeklaBeam PolyBeamToSpeckle(PolyBeam PolyBeam)
    {
      var speckleBeam = new TeklaBeam();
      var units = GetUnitsFromModel();
      var centerPolyCurve = PolyBeam.GetCenterLine(false);
      //var centerPolycurve = PolyBeam.GetCenterLinePolycurve();
      var pointList = new List<double> { };
      var polyLine = new Polycurve();
      foreach (Tekla.Structures.Geometry3d.Point point in centerPolyCurve)
      {
        pointList.Add(point.X);
        pointList.Add(point.Y);
        pointList.Add(point.Z);
      }
      speckleBeam.profile = GetBeamProfile(PolyBeam.Profile.ProfileString);
      speckleBeam.material = GetMaterial(PolyBeam.Material.MaterialString);
      speckleBeam.finish = PolyBeam.Finish;
      speckleBeam.classNumber = PolyBeam.Class;
      var beamCS = PolyBeam.GetCoordinateSystem();
      speckleBeam.position = GetPositioning(PolyBeam.Position);
      speckleBeam.alignmentVector = new Vector(beamCS.AxisY.X, beamCS.AxisY.Y, beamCS.AxisY.Z, units);
      speckleBeam.name = PolyBeam.Name;
      speckleBeam.baseLine = new Polyline(pointList, units);
      speckleBeam.TeklaBeamType = TeklaBeamType.PolyBeam;
      GetAllUserProperties(speckleBeam, PolyBeam);
      var solid = PolyBeam.GetSolid();
      speckleBeam.displayValue = new List<Mesh> { GetMeshFromSolid(solid) };
      var vol = new double();
      var area = new double();
      PolyBeam.GetReportProperty("VOLUME", ref vol);
      speckleBeam.volume = vol;
      PolyBeam.GetReportProperty("AREA", ref area);
      speckleBeam.area = area;

      return speckleBeam;
    }
  }
}