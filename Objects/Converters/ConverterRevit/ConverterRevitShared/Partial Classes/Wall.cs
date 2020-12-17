using Autodesk.Revit.DB;
using Objects.BuiltElements.Revit;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
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

      var revitWall = GetExistingElementByApplicationId(speckleWall.applicationId) as DB.Wall;

      var wallType = GetElementType<WallType>(speckleWall);
      Level level = null;
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

      //if it's a new element, we don't need to update certain properties
      bool isUpdate = true;
      if (revitWall == null)
      {
        isUpdate = false;
        revitWall = DB.Wall.Create(Doc, baseCurve, level.Id, structural);
      }
      if (revitWall == null)
      {
        ConversionErrors.Add(new Error { message = $"Failed to create wall ${speckleWall.applicationId}." });
        return null;
      }

      if (revitWall.WallType.Name != wallType.Name)
      {
        revitWall.ChangeTypeId(wallType.Id);
      }

      if (isUpdate)
      {
        //NOTE: updating an element location is quite buggy in Revit!
        //Let's say the first time an element is created its base point/curve is @ 10m and the Level is @ 0m
        //the element will be created @ 0m
        //but when this element is updated (let's say with no changes), it will jump @ 10m (unless there is a level change)!
        //to avoid this behavior we're always setting the previous location Z coordinate when updating an element
        //this means the Z coord of an element will only be set by its Level 
        //and by additional parameters as sill height, base offset etc
        var z = ((LocationCurve)revitWall.Location).Curve.GetEndPoint(0).Z;
        var offsetLine = baseCurve.CreateTransformed(Transform.CreateTranslation(new XYZ(0, 0, z)));
        ((LocationCurve)revitWall.Location).Curve = offsetLine;

        TrySetParam(revitWall, BuiltInParameter.WALL_BASE_CONSTRAINT, level);
      }

      if (speckleWall is RevitWall spklRevitWall)
      {
        if (spklRevitWall.flipped != revitWall.Flipped)
        {
          revitWall.Flip();
        }

        if (spklRevitWall.topLevel != null)
        {
          var topLevel = LevelToNative(spklRevitWall.topLevel);
          TrySetParam(revitWall, BuiltInParameter.WALL_HEIGHT_TYPE, topLevel);
        }
        else
        {
          TrySetParam(revitWall, BuiltInParameter.WALL_USER_HEIGHT_PARAM, speckleWall.height, speckleWall.units);
        }

        TrySetParam(revitWall, BuiltInParameter.WALL_BASE_OFFSET, spklRevitWall.baseOffset, speckleWall.units);
        TrySetParam(revitWall, BuiltInParameter.WALL_TOP_OFFSET, spklRevitWall.topOffset, speckleWall.units);

      }
      else // Set wall unconnected height.
      {
        TrySetParam(revitWall, BuiltInParameter.WALL_USER_HEIGHT_PARAM, speckleWall.height, speckleWall.units);
      }

      SetInstanceParameters(revitWall, speckleWall);

      var placeholders = new List<ApplicationPlaceholderObject>() {new ApplicationPlaceholderObject
      {
        applicationId = speckleWall.applicationId,
        ApplicationGeneratedId = revitWall.UniqueId,
        NativeObject = revitWall
      } };

      var hostedElements = SetHostedElements(speckleWall, revitWall);
      placeholders.AddRange(hostedElements);

      return placeholders;
    }

    public RevitWall WallToSpeckle(DB.Wall revitWall)
    {

      var baseGeometry = LocationToSpeckle(revitWall);
      if (baseGeometry is Geometry.Point)
      {
        ConversionErrors.Add(new Error { message = "Failed to convert wall by point. Currently not supported." });
        return null;
      }

      RevitWall speckleWall = new RevitWall();
      speckleWall.family = revitWall.WallType.FamilyName;
      speckleWall.type = revitWall.WallType.Name;
      speckleWall.baseLine = (ICurve)baseGeometry;
      speckleWall.level = ConvertAndCacheLevel(revitWall, BuiltInParameter.WALL_BASE_CONSTRAINT);
      speckleWall.topLevel = ConvertAndCacheLevel(revitWall, BuiltInParameter.WALL_HEIGHT_TYPE);
      speckleWall.height = GetParamValue<double>(revitWall, BuiltInParameter.WALL_USER_HEIGHT_PARAM);
      speckleWall.baseOffset = GetParamValue<double>(revitWall, BuiltInParameter.WALL_BASE_OFFSET);
      speckleWall.topOffset = GetParamValue<double>(revitWall, BuiltInParameter.WALL_TOP_OFFSET);
      speckleWall.structural = GetParamValue<bool>(revitWall, BuiltInParameter.WALL_STRUCTURAL_SIGNIFICANT);
      speckleWall.flipped = revitWall.Flipped;

      speckleWall["@displayMesh"] = GetWallDisplayMesh(revitWall);

      GetRevitParameters(speckleWall, revitWall, new List<string> { "WALL_USER_HEIGHT_PARAM", "WALL_BASE_OFFSET", "WALL_TOP_OFFSET", "WALL_BASE_CONSTRAINT",
      "WALL_HEIGHT_TYPE", "WALL_STRUCTURAL_SIGNIFICANT"});

      GetHostedElements(speckleWall, revitWall);

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