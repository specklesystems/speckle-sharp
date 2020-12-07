using Autodesk.Revit.DB;
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
        //when a curve is created its Z and gets adjusted to the level elevation!
        //make sure the new curve is at the same Z as the previous
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
          var heightParam = revitWall.get_Parameter(BuiltInParameter.WALL_USER_HEIGHT_PARAM);
          heightParam.Set(ScaleToNative(speckleWall.height, speckleWall.units));
        }

        var currentBaseOffset = (double)ParameterToSpeckle(revitWall.get_Parameter(BuiltInParameter.WALL_BASE_OFFSET));
        if (spklRevitWall.baseOffset != currentBaseOffset)
        {
          var boffsetparam = revitWall.get_Parameter(BuiltInParameter.WALL_BASE_OFFSET);
          boffsetparam.Set(0);
          boffsetparam.Set(ScaleToNative(spklRevitWall.baseOffset, speckleWall.units));
        }

        var currentTopOffset = (double)ParameterToSpeckle(revitWall.get_Parameter(BuiltInParameter.WALL_TOP_OFFSET));
        if (spklRevitWall.topOffset != currentTopOffset)
        {
          var toffsetParam = revitWall.get_Parameter(BuiltInParameter.WALL_TOP_OFFSET);
          toffsetParam.Set(0);
          toffsetParam.Set(ScaleToNative(spklRevitWall.topOffset, speckleWall.units));
        }
      }
      else // Set wall unconnected height.
      {
        var heightParam = revitWall.get_Parameter(BuiltInParameter.WALL_USER_HEIGHT_PARAM);
        heightParam.Set(ScaleToNative(speckleWall.height, speckleWall.units));
      }

      SetInstanceParameters(revitWall, speckleWall);

      var placeholders = new List<ApplicationPlaceholderObject>() {new ApplicationPlaceholderObject
      {
        applicationId = speckleWall.applicationId,
        ApplicationGeneratedId = revitWall.UniqueId,
        NativeObject = revitWall
      } };

      #region hosted elements creation

      if (speckleWall.elements != null)
      {
        CurrentHostElement = revitWall; // set the wall as the current host element.

        foreach (var obj in speckleWall.elements)
        {
          if (obj == null)
          {
            continue;
          }

          try
          {
            var res = ConvertToNative(obj);
            if (res is ApplicationPlaceholderObject apl)
            {
              placeholders.Add(apl);
            }
            else if (res is List<ApplicationPlaceholderObject> apls)
            {
              placeholders.AddRange(apls);
            }
          }
          catch
          {
            ConversionErrors.Add(new Error { message = $"Failed to create host element {obj.speckle_type} in {speckleWall.applicationId}." });
          }
        }

        CurrentHostElement = null; // unset the current host element.
      }

      #endregion

      Doc.Regenerate();

      return placeholders;
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

      if (baseGeometry is Geometry.Point)
      {
        ConversionErrors.Add(new Error { message = "Failed to convert wall by point. Currently not supported." });
        return null;
      }

      RevitWall speckleWall = new RevitWall();
      speckleWall.type = revitWall.WallType.Name;
      speckleWall.baseLine = (ICurve)baseGeometry;
      speckleWall.level = level;
      speckleWall.topLevel = topLevel;
      speckleWall.height = height;
      speckleWall.baseOffset = (double)ParameterToSpeckle(baseOffsetParam);
      speckleWall.topOffset = (double)ParameterToSpeckle(topOffsetParam);
      speckleWall.structural = (bool)ParameterToSpeckle(structural);
      speckleWall.flipped = revitWall.Flipped;

      speckleWall["@displayMesh"] = GetWallDisplayMesh(revitWall);

      AddCommonRevitProps(speckleWall, revitWall);

      #region hosted elements capture

      // TODO: perhaps move to generic method once patterns emerge (re other hosts).
      var hostedElements = revitWall.FindInserts(true, true, true, true);
      var hostedElementsList = new List<Base>();

      if (hostedElements != null)
      {
        var elementIndex = ContextObjects.FindIndex(obj => obj.applicationId == revitWall.UniqueId);
        if (elementIndex != -1)
        {
          ContextObjects.RemoveAt(elementIndex);
        }

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
          speckleWall.elements = hostedElementsList;
        }
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