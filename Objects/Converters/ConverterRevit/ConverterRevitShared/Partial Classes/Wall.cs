using Autodesk.Revit.DB;
using Objects.BuiltElements;
using Objects.BuiltElements.Revit;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using DB = Autodesk.Revit.DB;
using Mesh = Objects.Geometry.Mesh;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {

    public List<ApplicationPlaceholderObject> WallToNative(BuiltElements.Wall speckleWall)
    {
      if (speckleWall.baseLine == null)
      {
        throw new Exception("Only line based Walls are currently supported.");
      }

      var revitWall = GetExistingElementByApplicationId(((Base)speckleWall).applicationId) as DB.Wall;

      var wallType = GetElementType<WallType>(speckleWall);
      var level = GetFirstDocLevel();
      var structural = false;
      var baseCurve = CurveToNative(speckleWall.baseLine).get_Item(0);

      if (speckleWall is RevitWall speckleRevitWall)
      {
        level = LevelToNative(speckleRevitWall.level);
        structural = speckleRevitWall.structural;
      }
      else
      {
        level = LevelToNative(LevelFromCurve(baseCurve));
      }

      if(revitWall == null)
      {
        revitWall = DB.Wall.Create(Doc, baseCurve, level.Id, structural);
      }


      //try update existing wall
      var docObj = GetExistingElementByApplicationId(((Base)speckleWall).applicationId);
      if (docObj != null)
      {
        try
        {
          revitWall = (DB.Wall)docObj;

          TrySetParam(revitWall, BuiltInParameter.WALL_BASE_CONSTRAINT, level);
          //TrySetParam(revitWall, BuiltInParameter.WALL_TOP_OFFSET, )


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

      //if (speckleWall is RevitWallByLine rwbl)
      //{
      //  Level topLevel = LevelToNative(rwbl.topLevel);
      //  TrySetParam(revitWall, BuiltInParameter.WALL_HEIGHT_TYPE, topLevel);
      //}

      if (wallType != null)
      {
        revitWall.WallType = wallType;
      }

      if (speckleWall is RevitWall rw2 && rw2.flipped != revitWall.Flipped)
      {
        revitWall.Flip();
      }

      if (speckleWall is RevitWall myRevitWall)
      {
        // TODO: set bottom and top offsets.
      }


        SetElementParamsFromSpeckle(revitWall, speckleWall);

        // TODO: create nested children too
        //foreach (var obj in speckleRevitWall.hostedElements)
        //{
        //  var element = ConvertToNative(obj);
        //}
 

      var placeholder = new ApplicationPlaceholderObject
      {
        applicationId = ((Base)speckleWall).applicationId,
        ApplicationGeneratedId = revitWall.UniqueId,
        NativeObject = revitWall
      };

      return new List<ApplicationPlaceholderObject>() { placeholder };
    }

    public RevitWall WallToSpeckle(DB.Wall revitWall)
    {
      //REVIT PARAMS > SPECKLE PROPS
      var heightParam = revitWall.get_Parameter(BuiltInParameter.WALL_USER_HEIGHT_PARAM);

      var baseOffsetParam = revitWall.get_Parameter(BuiltInParameter.WALL_BASE_OFFSET);
      var topOffsetParam = revitWall.get_Parameter(BuiltInParameter.WALL_TOP_OFFSET);
      var baseLevelParam = revitWall.get_Parameter(BuiltInParameter.WALL_BASE_CONSTRAINT);
      var topLevelParam = revitWall.get_Parameter(BuiltInParameter.WALL_HEIGHT_TYPE);
      var structural = revitWall.get_Parameter(BuiltInParameter.WALL_STRUCTURAL_SIGNIFICANT); ;

      var baseGeometry = LocationToSpeckle(revitWall);
      var height = (double)ParameterToSpeckle(heightParam);
      var level = ConvertAndCacheLevel(baseLevelParam);
      var topLevel = ConvertAndCacheLevel(topLevelParam);

      RevitWall speckleWall = new RevitWall();

      //if (baseGeometry is Geometry.Point)
      //{
      //  speckleWall = new RevitWallByPoint()
      //  {
      //    type = revitWall.WallType.Name,
      //    basePoint = baseGeometry as Geometry.Point,
      //    level = level,
      //  };
      //}
      //else if (topLevel == null)
      //{
      //  speckleWall = new RevitWallUnconnected()
      //  {
      //    type = revitWall.WallType.Name,
      //    baseLine = baseGeometry as ICurve,
      //    level = level,
      //    height = height,
      //  };
      //}
      //else
      //{
      //  speckleWall = new RevitWallByLine()
      //  {
      //    type = revitWall.WallType.Name,
      //    baseLine = baseGeometry as ICurve,
      //    level = level,
      //    topLevel = topLevel,
      //    height = height,
      //  };
      //}


      speckleWall.baseOffset = ScaleToSpeckle((double)ParameterToSpeckle(baseOffsetParam));
      speckleWall.topOffset = ScaleToSpeckle((double)ParameterToSpeckle(topOffsetParam));
      speckleWall.structural = (bool)ParameterToSpeckle(structural);
      speckleWall.flipped = revitWall.Flipped;

      speckleWall["@displayMesh"] = GetWallDisplayMesh(revitWall);

      AddCommonRevitProps(speckleWall, revitWall);

      #region hosted elements capture

      var hostedElements = revitWall.FindInserts(true, true, true, true);
      var hostedElementsList = new List<Base>();

      ContextObjects.RemoveAt(ContextObjects.FindIndex(obj => obj.applicationId == revitWall.UniqueId));

      foreach (var elemId in hostedElements)
      {
        var element = Doc.GetElement(elemId);
        var isSelectedInContextObjects = ContextObjects.FindIndex(x => x.applicationId == element.UniqueId);

        if (isSelectedInContextObjects == -1)
        {
          continue;
        }

        ContextObjects.RemoveAt(isSelectedInContextObjects);

        try
        {
          var obj = ConvertToSpeckle(element);
          var xx = obj;

          if (obj != null)
          {
            hostedElementsList.Add(obj);
            ConvertedObjectsList.Add(obj.applicationId);
          }
        }
        catch (Exception e)
        {
          ConversionErrors.Add(new Error { message = e.Message, details = e.StackTrace });
        }
      }

      if (hostedElements.Count != 0)
      {
        //speckleWall.hostedElements = hostedElementsList;
      }

      #endregion

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