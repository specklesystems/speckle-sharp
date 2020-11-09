using Autodesk.Revit.DB;
using Objects.Revit;
using Objects.BuiltElements;
using System;
using System.Collections.Generic;

using DB = Autodesk.Revit.DB;

using Level = Objects.BuiltElements.Level;
using Mesh = Objects.Geometry.Mesh;
using Wall = Objects.BuiltElements.Wall;
using Element = Objects.BuiltElements.Element;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
    // TODO: (OLD)  A polycurve spawning multiple walls is not yet handled properly with diffing, etc.
    // TODO: (OLD)  Most probably, just get rid of the polyline wall handling stuff. It's rather annyoing and confusing...
    public DB.Wall WallToNative(Wall speckleWall)
    {

      if (speckleWall.baseLine == null)
      {
        throw new Exception("Only line based Walls are currently supported.");
      }

      DB.Wall revitWall = null;
      WallType wallType = null;
      DB.Level level = null;
      var structural = false;
      var baseCurve = CurveToNative(speckleWall.baseLine).get_Item(0); //TODO: support poliline/polycurve walls

      //comes from revit or schema builder, has these props
      if (speckleWall is RevitWall rw)
      {
        wallType = GetElementByName(typeof(WallType), rw.type) as WallType;
        level = LevelToNative(rw.level);
        structural = rw.structural;
      }
      else
      {
        level = LevelToNative(LevelFromCurve(baseCurve));
      }

      //try update existing wall
      var (docObj, stateObj) = GetExistingElementByApplicationId(speckleWall.applicationId, speckleWall.speckle_type);
      if (docObj != null)
      {
        try
        {
          revitWall = (DB.Wall)docObj;
          TrySetParam(revitWall, BuiltInParameter.WALL_BASE_CONSTRAINT, level);
          ((LocationCurve)revitWall.Location).Curve = baseCurve;
          if (wallType != null && revitWall.WallType.Name != wallType.Name)
            revitWall.ChangeTypeId(wallType.Id);
        }
        catch (Exception e)
        {
          //wall update failed, create a new one
        }
      }

      // create new wall
      if (revitWall == null)
      {
        revitWall = DB.Wall.Create(Doc, baseCurve, level.Id, structural);
      }

      if (speckleWall is RevitWallByLine rwbl)
      {
        DB.Level topLevel = LevelToNative(rwbl.topLevel);
        TrySetParam(revitWall, BuiltInParameter.WALL_HEIGHT_TYPE, topLevel);
      }

      if (wallType != null)
      {
        revitWall.WallType = wallType;
      }

      if (speckleWall is RevitWall rw2 && rw2.flipped != revitWall.Flipped)
      {
        revitWall.Flip();
      }

      if (speckleWall is IRevit item)
        SetElementParams(revitWall, item);

      return revitWall;
    }

    public IRevitElement WallToSpeckle(DB.Wall revitWall)
    {
      //REVIT PARAMS > SPECKLE PROPS
      var heightParam = revitWall.get_Parameter(BuiltInParameter.WALL_USER_HEIGHT_PARAM);

      //var baseOffsetParam = revitWall.get_Parameter(BuiltInParameter.WALL_BASE_OFFSET);
      //var topOffsetParam = revitWall.get_Parameter(BuiltInParameter.WALL_TOP_OFFSET);
      var baseLevelParam = revitWall.get_Parameter(BuiltInParameter.WALL_BASE_CONSTRAINT);
      var topLevelParam = revitWall.get_Parameter(BuiltInParameter.WALL_HEIGHT_TYPE);
      var structural = revitWall.get_Parameter(BuiltInParameter.WALL_STRUCTURAL_SIGNIFICANT); ;

      var baseGeometry = LocationToSpeckle(revitWall);
      var height = (double)ParameterToSpeckle(heightParam);
      var level = (RevitLevel)ParameterToSpeckle(baseLevelParam);
      var topLevel = (RevitLevel)ParameterToSpeckle(topLevelParam); //TODO: check if it works

      IRevitElement speckleWall = null;

      if (baseGeometry is Geometry.Point)
      {
        speckleWall = new RevitWallByPoint()
        {
          type = revitWall.WallType.Name,
          basePoint = baseGeometry as Geometry.Point,
          level = level,
        };
      }

      else if (topLevel == null)
      {
        speckleWall = new RevitWallUnconnected()
        {
          type = revitWall.WallType.Name,
          baseLine = baseGeometry as ICurve,
          level = level,
          height = height,
        };
      }
      else
      {
        speckleWall = new RevitWallByLine()
        {
          type = revitWall.WallType.Name,
          baseLine = baseGeometry as ICurve,
          level = level,
          topLevel = topLevel,
          height = height,
        };
      }

      ((Wall)speckleWall)["flipped"] = revitWall.Flipped;
      ((Wall)speckleWall)["structural"] = (bool)ParameterToSpeckle(structural);
      ((Wall)speckleWall).displayMesh = GetWallDisplayMesh(revitWall);

      AddCommonRevitProps(speckleWall, revitWall);

      return speckleWall;
    }

    private Mesh GetWallDisplayMesh(DB.Wall wall)
    {
      var grid = wall.CurtainGrid;
      var mesh = new Mesh();

      // meshing for walls in case they are curtain grids
      if (grid != null)
      {
        var mySolids = new List<Solid>();
        foreach (ElementId panelId in grid.GetPanelIds())
        {
          mySolids.AddRange(MeshUtils.GetElementSolids(Doc.GetElement(panelId)));
        }
        foreach (ElementId mullionId in grid.GetMullionIds())
        {
          mySolids.AddRange(MeshUtils.GetElementSolids(Doc.GetElement(mullionId)));
        }
        (mesh.faces, mesh.vertices) = MeshUtils.GetFaceVertexArrFromSolids(mySolids, Scale);
      }
      else
      {
        (mesh.faces, mesh.vertices) = MeshUtils.GetFaceVertexArrayFromElement(wall, Scale, new Options() { DetailLevel = ViewDetailLevel.Fine, ComputeReferences = false });
      }

      return mesh;
    }
  }
}