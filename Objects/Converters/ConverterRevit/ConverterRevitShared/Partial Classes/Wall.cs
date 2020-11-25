using Autodesk.Revit.DB;
using Objects.Revit;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using DB = Autodesk.Revit.DB;
using Mesh = Objects.Geometry.Mesh;
using Wall = Objects.BuiltElements.Wall;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {

    public List<ApplicationPlaceholderObject> WallToNative(IWall speckleWall)
    {

      if (speckleWall.baseLine == null)
      {
        throw new Exception("Only line based Walls are currently supported.");
      }

      DB.Wall revitWall = null;
      WallType wallType = GetElementType<WallType>(speckleWall);
      DB.Level level = null;
      var structural = false;
      var baseCurve = CurveToNative(speckleWall.baseLine).get_Item(0);

      //comes from revit or schema builder, has these props
      var speckleRevitWall = speckleWall as RevitWall;

      if (speckleRevitWall != null)
      {
        level = LevelToNative(speckleRevitWall.level);
        structural = speckleRevitWall.structural;
      }
      else
      {
        level = LevelToNative(LevelFromCurve(baseCurve));
      }

      //try update existing wall
      var docObj = GetExistingElementByApplicationId(speckleWall.applicationId);

      if (docObj != null)
      {
        try
        {
          revitWall = (DB.Wall)docObj;
          TrySetParam(revitWall, BuiltInParameter.WALL_BASE_CONSTRAINT, level);

          ((LocationCurve)revitWall.Location).Curve = baseCurve;

          if (wallType != null && revitWall.WallType.Name != wallType.Name)
          {
            revitWall.ChangeTypeId(wallType.Id);
          }
        }
        catch (Exception)
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

      if (speckleRevitWall != null)
      {
        SetElementParams(revitWall, speckleRevitWall);
      }

      // TODO: create nested children too

      var placeholder = new ApplicationPlaceholderObject
      {
        applicationId = speckleWall.applicationId,
        ApplicationGeneratedId = revitWall.Id.ToString()
      };

      return new List<ApplicationPlaceholderObject>() { placeholder };
    }

    public IRevit WallToSpeckle(DB.Wall revitWall)
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
      var level = ConvertAndCacheLevel(baseLevelParam);
      var topLevel = ConvertAndCacheLevel(topLevelParam);

      IRevit speckleWall = null;

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

      ((RevitWall)speckleWall)["flipped"] = revitWall.Flipped;
      ((RevitWall)speckleWall)["structural"] = (bool)ParameterToSpeckle(structural);
      ((RevitWall)speckleWall).displayMesh = GetWallDisplayMesh(revitWall);

      AddCommonRevitProps(speckleWall, revitWall);

      // TODO
      var hostedElements = revitWall.FindInserts(true, true, true, true);

      foreach (var elemId in hostedElements)
      {
        var element = Doc.GetElement(elemId);
        try
        {
          var obj = ConvertToSpeckle(element);
          var xx = obj;
        }
        catch (Exception e)
        {
          ConversionErrors.Add(new Error { message = e.Message, details = e.StackTrace });
        }
      }

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
          mySolids.AddRange(GetElementSolids(Doc.GetElement(panelId)));
        }
        foreach (ElementId mullionId in grid.GetMullionIds())
        {
          mySolids.AddRange(GetElementSolids(Doc.GetElement(mullionId)));
        }
        (mesh.faces, mesh.vertices) = GetFaceVertexArrFromSolids(mySolids);
      }
      else
      {
        (mesh.faces, mesh.vertices) = GetFaceVertexArrayFromElement(wall, new Options() { DetailLevel = ViewDetailLevel.Fine, ComputeReferences = false });
      }

      return mesh;
    }

  }
}