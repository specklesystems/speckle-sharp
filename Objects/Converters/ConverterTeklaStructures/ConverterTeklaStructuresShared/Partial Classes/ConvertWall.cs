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
using Objects.Building;
using Objects.Properties;



namespace Objects.Converter.TeklaStructures
{
  public partial class ConverterTeklaStructures
  {
    public Wall wallToSpeckle(Tekla.Structures.Model.Beam wall)
    {
      var units = GetUnitsFromModel();
      Wall speckleWall = new Wall();
      var WallCS = wall.GetCoordinateSystem();
      speckleWall.sourceApp = new TeklaStructuresProperties
      {

        alignmnetVector = new Vector(WallCS.AxisY.X, WallCS.AxisY.Y, WallCS.AxisY.Z, units),
        className = wall.Class,
        profile = GetBeamProfile(wall.Profile.ProfileString),
        material = GetMaterial(wall.Material.MaterialString),
        teklaPosition = GetPositioning(wall.Position),
        finish = wall.Finish,
      };
      var endPoint = wall.EndPoint;
      var startPoint = wall.StartPoint;
      Point speckleStartPoint = new Point(startPoint.X, startPoint.Y, startPoint.Z, units);
      Point speckleEndPoint = new Point(endPoint.X, endPoint.Y, endPoint.Z, units);
      speckleWall.baseCurve = new Line(speckleStartPoint, speckleEndPoint, units);
      speckleWall.applicationId = wall.Identifier.GUID.ToString();
      return speckleWall;
    }
  }
}

