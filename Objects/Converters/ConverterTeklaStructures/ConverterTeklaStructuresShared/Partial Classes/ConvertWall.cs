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
using Speckle.Core.Kits;



namespace Objects.Converter.TeklaStructures
{
  public partial class ConverterTeklaStructures
  {
    public void wallToNative (Wall Wall){
      if (!(Wall.baseCurve is Line))
      {
        Report.Log("Only line based Wall Elements are supported");
        return;
      }
      Line line = (Line)Wall.baseCurve;
      TSG.Point startPoint = new TSG.Point(line.start.x, line.start.y, line.start.z);
      TSG.Point endPoint = new TSG.Point(line.end.x, line.end.y, line.end.z);
      var myBeam = new Beam(startPoint, endPoint);
      if(Wall.sourceApp.name == HostApplications.TeklaStructures.Name ){
        SetPartPropertiesFromSourceApp((TeklaStructuresProperties)Wall.sourceApp,myBeam);
        myBeam.Insert();
      }
      myBeam.Insert();
    }
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
      speckleWall.displayValue = new List<Mesh> { GetMeshFromSolid(wall.GetSolid()) };
      return speckleWall;
    }
  }
}

