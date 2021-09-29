using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Objects.BuiltElements.Revit;
using Speckle.Core.Models;
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
        throw new Speckle.Core.Logging.SpeckleException("Only line based Walls are currently supported.");
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
        ConversionErrors.Add(new Exception($"Failed to create wall ${speckleWall.applicationId}."));
        return null;
      }

      //is structural update
      TrySetParam(revitWall, BuiltInParameter.WALL_STRUCTURAL_SIGNIFICANT, structural);


      if (revitWall.WallType.Name != wallType.Name)
      {
        
        revitWall.ChangeTypeId(wallType.Id);
      }

      if (isUpdate)
      {
        //NOTE: updating an element location can be buggy if the baseline and level elevation don't match
        //Let's say the first time an element is created its base point/curve is @ 10m and the Level is @ 0m
        //the element will be created @ 0m
        //but when this element is updated (let's say with no changes), it will jump @ 10m (unless there is a level change)!
        //to avoid this behavior we're moving the base curve to match the level elevation
        var newz = baseCurve.GetEndPoint(0).Z;
        var offset = level.Elevation - newz;
        var newCurve = baseCurve;
        if (Math.Abs(offset) > 0.0164042) // level and curve are not at the same height
        {
          newCurve = baseCurve.CreateTransformed(Transform.CreateTranslation(new XYZ(0, 0, offset)));
        }
        ((LocationCurve)revitWall.Location).Curve = newCurve;

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

      var placeholders = new List<ApplicationPlaceholderObject>()
      {
        new ApplicationPlaceholderObject
        {
        applicationId = speckleWall.applicationId,
        ApplicationGeneratedId = revitWall.UniqueId,
        NativeObject = revitWall
        }
      };

      var hostedElements = SetHostedElements(speckleWall, revitWall);
      placeholders.AddRange(hostedElements);

      return placeholders;
    }

    public Base WallToSpeckle(DB.Wall revitWall)
    {

      var baseGeometry = LocationToSpeckle(revitWall);
      if (baseGeometry is Geometry.Point)
      {
        return RevitElementToSpeckle(revitWall);
      }

      RevitWall speckleWall = new RevitWall();
      speckleWall.family = revitWall.WallType.FamilyName.ToString();
      speckleWall.type = revitWall.WallType.Name;
      speckleWall.baseLine = (ICurve)baseGeometry;
      speckleWall.level = ConvertAndCacheLevel(revitWall, BuiltInParameter.WALL_BASE_CONSTRAINT);
      speckleWall.topLevel = ConvertAndCacheLevel(revitWall, BuiltInParameter.WALL_HEIGHT_TYPE);
      speckleWall.height = GetParamValue<double>(revitWall, BuiltInParameter.WALL_USER_HEIGHT_PARAM);
      speckleWall.baseOffset = GetParamValue<double>(revitWall, BuiltInParameter.WALL_BASE_OFFSET);
      speckleWall.topOffset = GetParamValue<double>(revitWall, BuiltInParameter.WALL_TOP_OFFSET);
      speckleWall.structural = GetParamValue<bool>(revitWall, BuiltInParameter.WALL_STRUCTURAL_SIGNIFICANT);
      speckleWall.flipped = revitWall.Flipped;


      if (revitWall.CurtainGrid == null)
      {
        if ( revitWall.IsStackedWall )
        {
          var wallMembers = revitWall.GetStackedWallMemberIds().Select(id => (Wall)Doc.GetElement(id));
          speckleWall.elements = new List<Base>();
          foreach (  var wall in wallMembers )
            speckleWall.elements.Add(WallToSpeckle(wall));
        }

        speckleWall.displayMesh = GetElementDisplayMesh(revitWall,
          new Options() {DetailLevel = ViewDetailLevel.Fine, ComputeReferences = false});
      }
      else
      {
        // curtain walls have two meshes, one for panels and one for mullions
        // adding mullions as sub-elements so they can be correctly displayed in viewers etc
        (var panelsMesh, var mullionsMesh) = GetCurtainWallDisplayMesh(revitWall);
        speckleWall["renderMaterial"] = new Other.RenderMaterial() { opacity = 0.2, diffuse = System.Drawing.Color.AliceBlue.ToArgb() };
        speckleWall.displayMesh = panelsMesh;

        var mullions = new Base
        {
          [ "@displayMesh" ] = mullionsMesh,
          [ "renderMaterial" ] = new Other.RenderMaterial() {diffuse = System.Drawing.Color.DarkGray.ToArgb()}
        };
        speckleWall.elements = new List<Base> { mullions };

      }

      GetAllRevitParamsAndIds(speckleWall, revitWall, new List<string>
      {
        "WALL_USER_HEIGHT_PARAM",
        "WALL_BASE_OFFSET",
        "WALL_TOP_OFFSET",
        "WALL_BASE_CONSTRAINT",
        "WALL_HEIGHT_TYPE",
        "WALL_STRUCTURAL_SIGNIFICANT"
      });

      GetHostedElements(speckleWall, revitWall);

      return speckleWall;
    }

    private (Mesh, Mesh) GetCurtainWallDisplayMesh(DB.Wall wall)
    {
      var grid = wall.CurtainGrid;

      var meshPanels = new Mesh();
      var meshMullions = new Mesh();

      var solidPanels = new List<Solid>();
      var solidMullions = new List<Solid>();
      foreach (ElementId panelId in grid.GetPanelIds())
      {
        solidPanels.AddRange(GetElementSolids(Doc.GetElement(panelId)));
      }
      foreach (ElementId mullionId in grid.GetMullionIds())
      {
        solidMullions.AddRange(GetElementSolids(Doc.GetElement(mullionId)));
      }
      (meshPanels.faces, meshPanels.vertices) = GetFaceVertexArrFromSolids(solidPanels);
      (meshMullions.faces, meshMullions.vertices) = GetFaceVertexArrFromSolids(solidMullions);
      meshPanels.units = ModelUnits;
      meshMullions.units = ModelUnits;


      return (meshPanels, meshMullions);
    }

  }
}
