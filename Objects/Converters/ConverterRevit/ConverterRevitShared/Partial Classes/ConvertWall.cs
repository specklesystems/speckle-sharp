using System;
using System.Linq;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using Objects.Properties;
using Objects.BuiltElements.Revit;
using Speckle.Core.Models;
using DB = Autodesk.Revit.DB;
using Mesh = Objects.Geometry.Mesh;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
    public List<ApplicationPlaceholderObject> WallToNative(Building.Wall speckleWall)
    {
      if ( speckleWall.baseCurve == null )
        throw new Speckle.Core.Logging.SpeckleException(
          $"Failed to create wall ${speckleWall.applicationId}. Only line based Walls are currently supported.");

      var revitWall = GetExistingElementByApplicationId(speckleWall.applicationId) as DB.Wall;
      if ( revitWall != null && ReceiveMode == Speckle.Core.Kits.ReceiveMode.Ignore )
        return new List<ApplicationPlaceholderObject>
        {
          new ApplicationPlaceholderObject
          {
            applicationId = speckleWall.applicationId, ApplicationGeneratedId = revitWall.UniqueId,
            NativeObject = revitWall
          }
        };

      var wallType = GetElementType<WallType>(speckleWall);
      Level level;
      var baseCurve = CurveToNative(speckleWall.baseCurve).get_Item(0);

      level = ConvertLevelToRevit(speckleWall.baseLevel ?? Level2FromCurve(baseCurve));

      var isUpdate = true;
      if ( revitWall == null )
      {
        isUpdate = false;
        revitWall = DB.Wall.Create(Doc, baseCurve, level.Id, false);
      }

      if ( revitWall == null )
      {
        throw new Speckle.Core.Logging.SpeckleException($"Failed to create wall ${speckleWall.applicationId}.");
      }
      
      if ( revitWall.WallType.Name != wallType.Name )
        revitWall.ChangeTypeId(wallType.Id);

      if ( isUpdate )
      {
        //NOTE: updating an element location can be buggy if the baseline and level elevation don't match
        //Let's say the first time an element is created its base point/curve is @ 10m and the Level is @ 0m
        //the element will be created @ 0m
        //but when this element is updated (let's say with no changes), it will jump @ 10m (unless there is a level change)!
        //to avoid this behavior we're moving the base curve to match the level elevation
        var newz = baseCurve.GetEndPoint(0).Z;
        var offset = level.Elevation - newz;
        var newCurve = baseCurve;
        if ( Math.Abs(offset) > TOLERANCE ) // level and curve are not at the same height
          newCurve = baseCurve.CreateTransformed(Transform.CreateTranslation(new XYZ(0, 0, offset)));
        
        ( ( LocationCurve )revitWall.Location ).Curve = newCurve;
        TrySetParam(revitWall, BuiltInParameter.WALL_BASE_CONSTRAINT, level);
      }

      if ( speckleWall.flipped != revitWall.Flipped )
        revitWall.Flip();
      
      if ( speckleWall.topLevel != null )
      {
        var topLevel = ConvertLevelToRevit(speckleWall.topLevel);
        TrySetParam(revitWall, BuiltInParameter.WALL_HEIGHT_TYPE, topLevel);
      }
      else
      {
        TrySetParam(revitWall, BuiltInParameter.WALL_USER_HEIGHT_PARAM, speckleWall.height, speckleWall.units);
      }

      TrySetParam(revitWall, BuiltInParameter.WALL_BASE_OFFSET, speckleWall.baseOffset, speckleWall.units);
      TrySetParam(revitWall, BuiltInParameter.WALL_TOP_OFFSET, speckleWall.topOffset, speckleWall.units);
      TrySetParam(revitWall, BuiltInParameter.WALL_USER_HEIGHT_PARAM, speckleWall.height, speckleWall.units);
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
      
      Report.Log($"{( isUpdate ? "Updated" : "Created" )} Wall {revitWall.Id}");

      return placeholders;
    }

    public List<ApplicationPlaceholderObject> WallToNative(BuiltElements.Wall speckleWall)
    {
      if ( speckleWall.baseLine == null )
      {
        throw new Speckle.Core.Logging.SpeckleException(
          $"Failed to create wall ${speckleWall.applicationId}. Only line based Walls are currently supported.");
      }

      var revitWall = GetExistingElementByApplicationId(speckleWall.applicationId) as DB.Wall;
      if ( revitWall != null && ReceiveMode == Speckle.Core.Kits.ReceiveMode.Ignore )
        return new List<ApplicationPlaceholderObject>
        {
          new ApplicationPlaceholderObject
          {
            applicationId = speckleWall.applicationId, ApplicationGeneratedId = revitWall.UniqueId,
            NativeObject = revitWall
          }
        };
      ;

      var wallType = GetElementType<WallType>(speckleWall);
      Level level = null;
      var structural = false;
      var baseCurve = CurveToNative(speckleWall.baseLine).get_Item(0);

      if ( speckleWall is RevitWall speckleRevitWall )
      {
        level = ConvertLevelToRevit(speckleRevitWall.level);
        structural = speckleRevitWall.structural;
      }
      else
      {
        level = ConvertLevelToRevit(LevelFromCurve(baseCurve));
      }

      //if it's a new element, we don't need to update certain properties
      bool isUpdate = true;
      if ( revitWall == null )
      {
        isUpdate = false;
        revitWall = DB.Wall.Create(Doc, baseCurve, level.Id, structural);
      }

      if ( revitWall == null )
      {
        throw new Speckle.Core.Logging.SpeckleException($"Failed to create wall ${speckleWall.applicationId}.");
      }

      //is structural update
      TrySetParam(revitWall, BuiltInParameter.WALL_STRUCTURAL_SIGNIFICANT, structural);

      if ( revitWall.WallType.Name != wallType.Name )
        revitWall.ChangeTypeId(wallType.Id);

      if ( isUpdate )
      {
        //NOTE: updating an element location can be buggy if the baseline and level elevation don't match
        //Let's say the first time an element is created its base point/curve is @ 10m and the Level is @ 0m
        //the element will be created @ 0m
        //but when this element is updated (let's say with no changes), it will jump @ 10m (unless there is a level change)!
        //to avoid this behavior we're moving the base curve to match the level elevation
        var newz = baseCurve.GetEndPoint(0).Z;
        var offset = level.Elevation - newz;
        var newCurve = baseCurve;
        if ( Math.Abs(offset) > TOLERANCE ) // level and curve are not at the same height
        {
          newCurve = baseCurve.CreateTransformed(Transform.CreateTranslation(new XYZ(0, 0, offset)));
        }

        ( ( LocationCurve )revitWall.Location ).Curve = newCurve;

        TrySetParam(revitWall, BuiltInParameter.WALL_BASE_CONSTRAINT, level);
      }

      if ( speckleWall is RevitWall spklRevitWall )
      {
        if ( spklRevitWall.flipped != revitWall.Flipped )
        {
          revitWall.Flip();
        }

        if ( spklRevitWall.topLevel != null )
        {
          var topLevel = ConvertLevelToRevit(spklRevitWall.topLevel);
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
      
      Report.Log($"{( isUpdate ? "Updated" : "Created" )} Wall {revitWall.Id}");
      
      return placeholders;
    }

    public Base WallToSpeckle(DB.Wall revitWall)
    {
      var baseGeometry = LocationToSpeckle(revitWall);
      if ( baseGeometry is Geometry.Point )
      {
        return RevitElementToSpeckle(revitWall);
      }

      var speckleWall = new Building.Wall
      {
        applicationId = revitWall.UniqueId,
        baseCurve = ( ICurve )baseGeometry,
        baseLevel = ConvertAndCacheLevel2(revitWall, BuiltInParameter.WALL_BASE_CONSTRAINT),
        height = GetParamValue<double>(revitWall, BuiltInParameter.WALL_USER_HEIGHT_PARAM),
        topLevel = ConvertAndCacheLevel2(revitWall, BuiltInParameter.WALL_HEIGHT_TYPE),
        baseOffset = GetParamValue<double>(revitWall, BuiltInParameter.WALL_BASE_OFFSET),
        topOffset = GetParamValue<double>(revitWall, BuiltInParameter.WALL_TOP_OFFSET),
        flipped = revitWall.Flipped
      };

      speckleWall.sourceApp = new RevitProperties
      {
        family = revitWall.WallType.FamilyName,
        type = revitWall.WallType.Name,
        elementId = revitWall.Id.ToString(),
        props = GetRevitParams(speckleWall, revitWall, new List<string>
        {
          "WALL_USER_HEIGHT_PARAM",
          "WALL_BASE_OFFSET",
          "WALL_TOP_OFFSET",
          "WALL_BASE_CONSTRAINT",
          "WALL_HEIGHT_TYPE",
        })
      };

      if ( revitWall.CurtainGrid == null )
      {
        if ( revitWall.IsStackedWall )
        {
          var wallMembers = revitWall.GetStackedWallMemberIds()
            .Select(id => ( DB.Wall )revitWall.Document.GetElement(id));
          speckleWall.elements = new List<Base>();
          foreach ( var wall in wallMembers )
            speckleWall.elements.Add(WallToSpeckle(wall));
        }

        speckleWall.displayValue = GetElementDisplayMesh(revitWall,
          new Options() { DetailLevel = ViewDetailLevel.Fine, ComputeReferences = false });
      }
      else
      {
        // curtain walls have two meshes, one for panels and one for mullions
        // adding mullions as sub-elements so they can be correctly displayed in viewers etc
        var (panelsMesh, mullionsMesh) = GetCurtainWallDisplayMesh(revitWall);
        speckleWall[ "renderMaterial" ] = new Other.RenderMaterial()
          { opacity = 0.2, diffuse = System.Drawing.Color.AliceBlue.ToArgb() };
        speckleWall.displayValue = panelsMesh;

        var elements = new List<Base>();
        if ( mullionsMesh.Count > 0 ) //Only add mullions object if they have meshes 
        {
          elements.Add(new Base
          {
            [ "@displayValue" ] = mullionsMesh
          });
        }

        speckleWall.elements = elements;
      }

      GetHostedElements(speckleWall, revitWall);
      Report.Log($"Converted Wall {revitWall.Id}");

      return speckleWall;
    }

    private (List<Mesh>, List<Mesh>) GetCurtainWallDisplayMesh(DB.Wall wall)
    {
      var grid = wall.CurtainGrid;

      var solidPanels = new List<Solid>();
      var solidMullions = new List<Solid>();
      foreach ( ElementId panelId in grid.GetPanelIds() )
      {
        //TODO: sort these so we consistently get sub-elements from the wall element in case also individual sub-elements are sent
        if ( SubelementIds.Contains(panelId) )
          continue;
        SubelementIds.Add(panelId);
        solidPanels.AddRange(GetElementSolids(wall.Document.GetElement(panelId)));
      }

      foreach ( ElementId mullionId in grid.GetMullionIds() )
      {
        //TODO: sort these so we consistently get sub-elements from the wall element in case also individual sub-elements are sent
        if ( SubelementIds.Contains(mullionId) )
          continue;
        SubelementIds.Add(mullionId);
        solidMullions.AddRange(GetElementSolids(wall.Document.GetElement(mullionId)));
      }

      var meshPanels = GetMeshesFromSolids(solidPanels, wall.Document);
      var meshMullions = GetMeshesFromSolids(solidMullions, wall.Document);

      return ( meshPanels, meshMullions );
    }

    //this is to prevent duplicated panels & mullions from being sent in curtain walls
    //might need improvement in the future
    //see https://github.com/specklesystems/speckle-sharp/issues/1197
    private List<ElementId> SubelementIds = new List<ElementId>();
  }
}