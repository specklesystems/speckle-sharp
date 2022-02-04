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
using TSG  = Tekla.Structures.Geometry3d;
using System.Collections;
using StructuralUtilities.PolygonMesher;

namespace Objects.Converter.TeklaStructures
{
  public partial class ConverterTeklaStructures
  {
  public void BeamToNative(BE.Beam beam){
      if(!(beam.baseLine is Line)){
      }
      Line line = (Line)beam.baseLine;
      TSG.Point startPoint = new TSG.Point(line.start.x, line.start.y, line.start.z);
      TSG.Point endPoint = new TSG.Point(line.end.x, line.end.y, line.end.z);
      Beam myBeam = new Beam(startPoint, endPoint);

      if(beam is TeklaBeam){
        var teklaBeam = (TeklaBeam)beam;
        myBeam.Material.MaterialString = teklaBeam.material.name;
        myBeam.Profile.ProfileString = teklaBeam.profile.name;
        myBeam.Class = teklaBeam.classNumber;
        myBeam.Finish = teklaBeam.finish;
        myBeam.Name = teklaBeam.name;

      }
      myBeam.Insert();
      Model.CommitChanges();

      //return TeklaBeam;
  }
    public BE.Beam BeamToSpeckle(Tekla.Structures.Model.Beam beam)
    {
      var speckleBeam = new TeklaBeam();
      //TO DO: Support for curved beams goes in here as well + twin beams

      var endPoint = beam.EndPoint;
      var startPoint = beam.StartPoint;
      var units = GetUnitsFromModel();

      Point speckleStartPoint = new Point(startPoint.X, startPoint.Y, startPoint.Z,units);
      Point speckleEndPoint = new Point(endPoint.X, endPoint.Y, endPoint.Z,units);
      speckleBeam.baseLine = new Line(speckleStartPoint, speckleEndPoint,units);

      speckleBeam.profile = GetProfile(beam.Profile.ProfileString);
      speckleBeam.material = GetMaterial(beam.Material.MaterialString);
      var beamCS = beam.GetCoordinateSystem();
      speckleBeam.alignmentVector = new Vector(beamCS.AxisY.X, beamCS.AxisY.Y, beamCS.AxisY.Z,units);
      speckleBeam.finish = beam.Finish;
      speckleBeam.classNumber = beam.Class;
      speckleBeam.name = beam.Name;

      GetAllUserProperties(speckleBeam, beam);

      var solid = beam.GetSolid();
      speckleBeam.displayMesh = GetMeshFromSolid(solid);
      return speckleBeam;
    }
  }
}