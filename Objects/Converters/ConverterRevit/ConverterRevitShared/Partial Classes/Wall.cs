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

      if (revitWall == null)
      {
        revitWall = DB.Wall.Create(Doc, baseCurve, level.Id, structural);
      }

      if (revitWall == null)
      {
        ConversionErrors.Add(new Error { message = $"Failed to create wall ${speckleWall.applicationId}." });
        return null;
      }

      var ocrvStart = ((LocationCurve)revitWall.Location).Curve.GetEndPoint(0);
      var ocrvEnd = ((LocationCurve)revitWall.Location).Curve.GetEndPoint(1);
      var ncrvStart = baseCurve.GetEndPoint(0);
      var ncrvEnd = baseCurve.GetEndPoint(1);

      // Note: setting a base offset on a wall modifies its location curve. As such, to distinguish between an old curve and a new one, we need to
      // remove any existing base offset before comparing it to the original one. And yes, of course, we need to check within a tolerance.
      var cbo = (double)revitWall.get_Parameter(BuiltInParameter.WALL_BASE_OFFSET).AsDouble(); // note: we're using raw internal units, no need for conversions
      if (Math.Abs(ocrvStart.X - ncrvStart.X) > 0.01 || Math.Abs(ocrvStart.Y - ncrvStart.Y) > 0.01 || Math.Abs(ocrvStart.Z + cbo - ncrvStart.Z) > 0.01 ||
        Math.Abs(ocrvEnd.X - ncrvEnd.X) > 0.01 || Math.Abs(ocrvEnd.Y - ncrvEnd.Y) > 0.01 || Math.Abs(ocrvEnd.Z + cbo - ncrvEnd.Z) > 0.01)
      {
        revitWall.get_Parameter(BuiltInParameter.WALL_BASE_OFFSET).Set(0); // note: always reset the base offset before setting a new location curve, otherwise it's applied twice.
        ((LocationCurve)revitWall.Location).Curve = baseCurve;
      }

      TrySetParam(revitWall, BuiltInParameter.WALL_BASE_CONSTRAINT, level);

      if (wallType != null && revitWall.WallType.Name != wallType.Name)
      {
        revitWall.ChangeTypeId(wallType.Id);
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

      SetElementParamsFromSpeckle(revitWall, speckleWall); // This takes very long and doesn't do much. IMHO we should stop supporting it.

      var placeholders = new List<ApplicationPlaceholderObject>() {new ApplicationPlaceholderObject
      {
        applicationId = speckleWall.applicationId,
        ApplicationGeneratedId = revitWall.UniqueId,
        NativeObject = revitWall
      } };

      #region hosted elements creation

      var hostedElements = speckleWall["hostedElements"] as List<Base>;
      if (hostedElements != null)
      {
        CurrentHostElement = revitWall; // set the wall as the current host element.

        foreach (var obj in hostedElements)
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
          speckleWall.hostedElements = hostedElementsList;
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