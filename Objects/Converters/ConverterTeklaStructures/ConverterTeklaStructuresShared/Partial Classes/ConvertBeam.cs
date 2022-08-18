using System;
using System.Collections.Generic;
using Objects.Geometry;
using Objects.Structural.Geometry;
using Objects.Structural.Analysis;
using Speckle.Core.Models;
using BE = Objects.BuiltElements;
using Objects.BuiltElements.TeklaStructures;
using System.Linq;
using Tekla.Structures.Model;
using Tekla.Structures.Solid;
using TSG = Tekla.Structures.Geometry3d;
using System.Collections;
using StructuralUtilities.PolygonMesher;

namespace Objects.Converter.TeklaStructures
{
  public partial class ConverterTeklaStructures
  {
    public void BeamToNative(BE.Beam beam)
    {


      if (beam is TeklaBeam)
      {

        var teklaBeam = (TeklaBeam)beam;
        switch (teklaBeam.TeklaBeamType)
        {
          case TeklaBeamType.Beam:
            if (!(beam.baseLine is Line))
            {
            }
            Line line = (Line)beam.baseLine;
            TSG.Point startPoint = new TSG.Point(line.start.x, line.start.y, line.start.z);
            TSG.Point endPoint = new TSG.Point(line.end.x, line.end.y, line.end.z);
            var myBeam = new Beam(startPoint, endPoint);
            SetPartProperties(myBeam, teklaBeam);
            if (!IsProfileValid(myBeam.Profile.ProfileString))
            {
                Report.Log($"{myBeam.Profile.ProfileString} not in model catalog. Cannot place object {beam.id}");
                return;
            }
            myBeam.Insert();
            //Model.CommitChanges();
            break;
          case TeklaBeamType.PolyBeam:
            Polyline polyline = (Polyline)beam.baseLine;
            var polyBeam = new PolyBeam();
            ToNativeContourPlate(polyline, polyBeam.Contour);
            SetPartProperties(polyBeam, teklaBeam);
            if (!IsProfileValid(polyBeam.Profile.ProfileString))
            {
                Report.Log($"{polyBeam.Profile.ProfileString} not in model catalog. Cannot place object {beam.id}");
                return;
            }
            polyBeam.Insert();
            //Model.CommitChanges();
            break;
          case TeklaBeamType.SpiralBeam:
            Polyline polyline2 = (Polyline)beam.baseLine;
            var teklaSpiralBeam = (Objects.BuiltElements.TeklaStructures.SpiralBeam)teklaBeam;
            var startPt = new TSG.Point(teklaSpiralBeam.startPoint.x, teklaSpiralBeam.startPoint.y, teklaSpiralBeam.startPoint.z);
            var rotatAxisPt1 = new TSG.Point(teklaSpiralBeam.rotationAxisPt1.x, teklaSpiralBeam.rotationAxisPt1.y, teklaSpiralBeam.rotationAxisPt1.z);
            var rotatAxisPt2 = new TSG.Point(teklaSpiralBeam.rotationAxisPt2.x, teklaSpiralBeam.rotationAxisPt2.y, teklaSpiralBeam.rotationAxisPt2.z);
            var totalRise = teklaSpiralBeam.totalRise;
            var rotationAngle = teklaSpiralBeam.rotationAngle;
            var twistAngleStart = teklaSpiralBeam.twistAngleStart;
            var twistAngleEnd = teklaSpiralBeam.twistAngleEnd;
            var spiralBeam = new Tekla.Structures.Model.SpiralBeam(startPt, rotatAxisPt1, rotatAxisPt2, totalRise, rotationAngle, twistAngleStart, twistAngleEnd);
            SetPartProperties(spiralBeam, teklaBeam);
            if (!IsProfileValid(spiralBeam.Profile.ProfileString))
            {
                Report.Log($"{spiralBeam.Profile.ProfileString} not in model catalog. Cannot place object {beam.id}");
                return;
            }
            spiralBeam.Insert();
            //Model.CommitChanges();
            break;
        }
      }
      else
      {
        if (!(beam.baseLine is Line))
        {
        }
        Line line = (Line)beam.baseLine;
        TSG.Point startPoint = new TSG.Point(line.start.x, line.start.y, line.start.z);
        TSG.Point endPoint = new TSG.Point(line.end.x, line.end.y, line.end.z);
        var myBeam = new Beam(startPoint, endPoint);
        myBeam.Insert();
        //Model.CommitChanges();
      }
    }

    public void SetPartProperties(Part part, TeklaBeam teklaBeam)
    {
      part.Material.MaterialString = teklaBeam.material.name;
      part.Profile.ProfileString = teklaBeam.profile.name;
      part.Class = teklaBeam.classNumber;
      part.Finish = teklaBeam.finish;
      part.Name = teklaBeam.name;
            part.Position = SetPositioning(teklaBeam.position);
    }
        
    public TeklaBeam BeamToSpeckle(Tekla.Structures.Model.Beam beam)
    {
      var speckleBeam = new TeklaBeam();
      //TO DO: Support for curved beams goes in here as well + twin beams

      var endPoint = beam.EndPoint;
      var startPoint = beam.StartPoint;
      var units = GetUnitsFromModel();

      Point speckleStartPoint = new Point(startPoint.X, startPoint.Y, startPoint.Z, units);
      Point speckleEndPoint = new Point(endPoint.X, endPoint.Y, endPoint.Z, units);
      speckleBeam.baseLine = new Line(speckleStartPoint, speckleEndPoint, units);
      speckleBeam.baseLine.length = Math.Sqrt(Math.Pow((startPoint.X - endPoint.X),2)+ Math.Pow((startPoint.Y - endPoint.Y), 2)+ Math.Pow((startPoint.Z - endPoint.Z), 2));
      speckleBeam.profile = GetBeamProfile(beam.Profile.ProfileString);
      speckleBeam.material = GetMaterial(beam.Material.MaterialString);
      var beamCS = beam.GetCoordinateSystem();
      speckleBeam.position = GetPositioning(beam.Position);
      speckleBeam.alignmentVector = new Vector(beamCS.AxisY.X, beamCS.AxisY.Y, beamCS.AxisY.Z, units);
      speckleBeam.finish = beam.Finish;
      speckleBeam.classNumber = beam.Class;
      speckleBeam.name = beam.Name;
      speckleBeam.applicationId = beam.Identifier.GUID.ToString();
      speckleBeam.TeklaBeamType = TeklaBeamType.Beam;
      var vol = new double();
      var area = new double();
      beam.GetReportProperty("VOLUME", ref vol);
      speckleBeam.volume = vol;
      beam.GetReportProperty("AREA", ref area);
      speckleBeam.area = area;
      
      var rebars = beam.GetReinforcements();
      if (rebars != null)
      {
        foreach (var rebar in rebars)
        {
           if (rebar is RebarGroup) {speckleBeam.rebars =  RebarGroupToSpeckle((RebarGroup)rebar); }

        }
      }

      GetAllUserProperties(speckleBeam, beam);

      var solid = beam.GetSolid();
      speckleBeam.displayValue = new List<Mesh>{ GetMeshFromSolid(solid)};
      return speckleBeam;
    }
    /// <summary>
    /// Create beam without display mesh for boolean parts
    /// </summary>
    /// <param name="beam"></param>
    /// <returns></returns>
    public TeklaBeam AntiBeamToSpeckle(Tekla.Structures.Model.Beam beam)
    {
      var speckleBeam = new TeklaBeam();
      //TO DO: Support for curved beams goes in here as well + twin beams

      var endPoint = beam.EndPoint;
      var startPoint = beam.StartPoint;
      var units = GetUnitsFromModel();

      Point speckleStartPoint = new Point(startPoint.X, startPoint.Y, startPoint.Z, units);
      Point speckleEndPoint = new Point(endPoint.X, endPoint.Y, endPoint.Z, units);
      speckleBeam.baseLine = new Line(speckleStartPoint, speckleEndPoint, units);

      speckleBeam.profile = GetBeamProfile(beam.Profile.ProfileString);
      speckleBeam.material = GetMaterial(beam.Material.MaterialString);
      var beamCS = beam.GetCoordinateSystem();
      speckleBeam.position = GetPositioning(beam.Position);
      speckleBeam.alignmentVector = new Vector(beamCS.AxisY.X, beamCS.AxisY.Y, beamCS.AxisY.Z, units);
      speckleBeam.classNumber = beam.Class;
      speckleBeam.name = beam.Name;
      speckleBeam.TeklaBeamType = TeklaBeamType.Beam;
      speckleBeam.applicationId = beam.Identifier.GUID.ToString();
      var vol = new double();
      var area = new double();
      beam.GetReportProperty("VOLUME", ref vol);
      speckleBeam.volume = vol;
      beam.GetReportProperty("AREA", ref area);
      speckleBeam.area = area;

      speckleBeam["units"] = units;

      return speckleBeam;
    }
  }
}