using Autodesk.Revit.DB;
using Objects.BuiltElements.Revit;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using DB = Autodesk.Revit.DB;
using Mesh = Objects.Geometry.Mesh;
using System.Text.RegularExpressions;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
    // CAUTION: these strings need to have the same values as in the connector
    const string StructuralWalls = "Structural Walls";
    const string ArchitecturalWalls = "Achitectural Walls";

    public List<ApplicationPlaceholderObject> WallToNative(BuiltElements.Wall speckleWall)
    {
      if (speckleWall.baseLine == null)
      {
        throw new Speckle.Core.Logging.SpeckleException($"Failed to create wall ${speckleWall.applicationId}. Only line based Walls are currently supported.");
      }

      var revitWall = GetExistingElementByApplicationId(speckleWall.applicationId) as DB.Wall;
      if (revitWall != null && ReceiveMode == Speckle.Core.Kits.ReceiveMode.Ignore)
        return new List<ApplicationPlaceholderObject> { new ApplicationPlaceholderObject { applicationId = speckleWall.applicationId, ApplicationGeneratedId = revitWall.UniqueId, NativeObject = revitWall } }; ;

      var wallType = GetElementType<WallType>(speckleWall);
      Level level = null;
      var structural = false;
      var baseCurve = CurveToNative(speckleWall.baseLine).get_Item(0);
      List<string> joinSettings = new List<string>();
      if (Settings.ContainsKey("disallow-join"))
      {
        joinSettings = new List<string>(Regex.Split(Settings["disallow-join"], @"\,\ "));
      }

      if (speckleWall is RevitWall speckleRevitWall)
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
      if (revitWall == null)
      {
        isUpdate = false;
        revitWall = DB.Wall.Create(Doc, baseCurve, level.Id, structural);
        if (joinSettings.Contains(StructuralWalls) && structural)
        {
          WallUtils.DisallowWallJoinAtEnd(revitWall, 0);
          WallUtils.DisallowWallJoinAtEnd(revitWall, 1);
        }
        if (joinSettings.Contains(ArchitecturalWalls) && !structural)
        {
          WallUtils.DisallowWallJoinAtEnd(revitWall, 0);
          WallUtils.DisallowWallJoinAtEnd(revitWall, 1);
        }
      }
      if (revitWall == null)
      {
        throw new Speckle.Core.Logging.SpeckleException($"Failed to create wall ${speckleWall.applicationId}.");
      }

      //is structural update
      TrySetParam(revitWall, BuiltInParameter.WALL_STRUCTURAL_SIGNIFICANT, structural);

      if (revitWall.WallType.Name != wallType.Name)
        revitWall.ChangeTypeId(wallType.Id);

      if (isUpdate)
      {
        // walls behave very strangly while joined
        // if a wall is joined and you try to move it to a location where it isn't touching the joined wall,
        // then it will move only one end of the wall and the other will stay joined
        // therefore, disallow joins while changing the wall and then reallow the joins if need be
        WallUtils.DisallowWallJoinAtEnd(revitWall, 0);
        WallUtils.DisallowWallJoinAtEnd(revitWall, 1);

        //NOTE: updating an element location can be buggy if the baseline and level elevation don't match
        //Let's say the first time an element is created its base point/curve is @ 10m and the Level is @ 0m
        //the element will be created @ 0m
        //but when this element is updated (let's say with no changes), it will jump @ 10m (unless there is a level change)!
        //to avoid this behavior we're moving the base curve to match the level elevation
        var newz = baseCurve.GetEndPoint(0).Z;
        var offset = level.Elevation - newz;
        var newCurve = baseCurve;
        if (Math.Abs(offset) > TOLERANCE) // level and curve are not at the same height
        {
          newCurve = baseCurve.CreateTransformed(Transform.CreateTranslation(new XYZ(0, 0, offset)));
        }
        ((LocationCurve)revitWall.Location).Curve = newCurve;

        TrySetParam(revitWall, BuiltInParameter.WALL_BASE_CONSTRAINT, level);


        // now that we've moved the wall, rejoin the wall ends
        if (!joinSettings.Contains(StructuralWalls) && structural)
        {
          WallUtils.AllowWallJoinAtEnd(revitWall, 0);
          WallUtils.AllowWallJoinAtEnd(revitWall, 1);
        }
        if (!joinSettings.Contains(ArchitecturalWalls) && !structural)
        {
          WallUtils.AllowWallJoinAtEnd(revitWall, 0);
          WallUtils.AllowWallJoinAtEnd(revitWall, 1);
        }
      }

      if (speckleWall is RevitWall spklRevitWall)
      {
        if (spklRevitWall.flipped != revitWall.Flipped)
        {
          revitWall.Flip();
        }

        if (spklRevitWall.topLevel != null)
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


      Report.Log($"{(isUpdate ? "Updated" : "Created")} Wall {revitWall.Id}");


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
        if (revitWall.IsStackedWall)
        {
          var wallMembers = revitWall.GetStackedWallMemberIds().Select(id => (Wall)revitWall.Document.GetElement(id));
          speckleWall.elements = new List<Base>();
          foreach (var wall in wallMembers)
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
        speckleWall["renderMaterial"] = new Other.RenderMaterial() { opacity = 0.2, diffuse = System.Drawing.Color.AliceBlue.ToArgb() };
        speckleWall.displayValue = panelsMesh;

        var elements = new List<Base>();
        if (mullionsMesh.Count > 0) //Only add mullions object if they have meshes 
        {
          elements.Add(new Base
          {
            ["@displayValue"] = mullionsMesh
          });
        }

        speckleWall.elements = elements;

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
      Report.Log($"Converted Wall {revitWall.Id}");

      return speckleWall;
    }

    private (List<Mesh>, List<Mesh>) GetCurtainWallDisplayMesh(DB.Wall wall)
    {
      var grid = wall.CurtainGrid;

      var solidPanels = new List<Solid>();
      var solidMullions = new List<Solid>();
      foreach (ElementId panelId in grid.GetPanelIds())
      {
        //TODO: sort these so we consistently get sub-elements from the wall element in case also individual sub-elements are sent
        if (SubelementIds.Contains(panelId))
          continue;
        SubelementIds.Add(panelId);
        solidPanels.AddRange(GetElementSolids(wall.Document.GetElement(panelId)));
      }
      foreach (ElementId mullionId in grid.GetMullionIds())
      {
        //TODO: sort these so we consistently get sub-elements from the wall element in case also individual sub-elements are sent
        if (SubelementIds.Contains(mullionId))
          continue;
        SubelementIds.Add(mullionId);
        solidMullions.AddRange(GetElementSolids(wall.Document.GetElement(mullionId)));
      }

      var meshPanels = GetMeshesFromSolids(solidPanels, wall.Document);
      var meshMullions = GetMeshesFromSolids(solidMullions, wall.Document);

      return (meshPanels, meshMullions);
    }

    //this is to prevent duplicated panels & mullions from being sent in curtain walls
    //might need improvement in the future
    //see https://github.com/specklesystems/speckle-sharp/issues/1197
    private List<ElementId> SubelementIds = new List<ElementId>();

  }
}
