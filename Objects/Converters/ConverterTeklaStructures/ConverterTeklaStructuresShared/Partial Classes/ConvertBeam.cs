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

    public BE.Beam BeamToSpeckle(Tekla.Structures.Model.Beam beam)
    {
      var speckleBeam = new BE.Beam();
      //TO DO: Support for curved beams goes in here as well + twin beams + concrete and materials
      var endPoint = beam.EndPoint;
      var startPoint = beam.StartPoint;

      Point speckleStartPoint = new Point(startPoint.X, startPoint.Y, startPoint.Z);
      Point speckleEndPoint = new Point(endPoint.X, endPoint.Y, endPoint.Z);
      speckleBeam.baseLine = new Line(speckleStartPoint, speckleEndPoint);
      var profile = beam.Profile.ProfileString;

      var solid = beam.GetSolid();
      speckleBeam.displayMesh = GetMeshFromSolid(solid);
      return speckleBeam;
    }
  }
}