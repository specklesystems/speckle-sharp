using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Autodesk.Revit.DB;
using Objects.BuiltElements.Revit;
using Speckle.Core.Models;
using DB = Autodesk.Revit.DB;
using Mesh = Objects.Geometry.Mesh;
#if !REVIT2020 && !REVIT2021
using OG = Objects.Geometry;
using RevitSharedResources.Models;
using System.Collections;
#endif

namespace Objects.Converter.Revit;

public partial class ConverterRevit
{
  // CAUTION: these strings need to have the same values as in the connector
  const string StructuralWalls = "Structural Walls";
  const string ArchitecturalWalls = "Achitectural Walls";

  public ApplicationObject WallToNative(BuiltElements.Wall speckleWall)
  {
    var revitWall = GetExistingElementByApplicationId(speckleWall.applicationId) as DB.Wall;
    var appObj = new ApplicationObject(speckleWall.id, speckleWall.speckle_type)
    {
      applicationId = speckleWall.applicationId
    };

    // skip if element already exists in doc & receive mode is set to ignore
    if (IsIgnore(revitWall, appObj))
    {
      return appObj;
    }

    if (speckleWall.baseLine == null)
    {
      appObj.Update(status: ApplicationObject.State.Failed, logItem: "Baseline was null");
      return appObj;
    }

    var wallType = GetElementType<WallType>(speckleWall, appObj, out bool isExactMatch);
    if (wallType == null)
    {
      appObj.Update(status: ApplicationObject.State.Failed);
      return appObj;
    }

    var baseCurve = CurveToNative(speckleWall.baseLine).get_Item(0);

    List<string> joinSettings = new();

    if (Settings.ContainsKey("disallow-join") && !string.IsNullOrEmpty(Settings["disallow-join"]))
    {
      joinSettings = new List<string>(Regex.Split(Settings["disallow-join"], @"\,\ "));
    }

    var levelState = ApplicationObject.State.Unknown;
    double baseOffset = 0.0;
    Level level =
      (speckleWall.level != null)
        ? ConvertLevelToRevit(speckleWall.level, out levelState)
        : ConvertLevelToRevit(baseCurve, out levelState, out baseOffset);

    var structural = false;
    if (speckleWall is RevitWall speckleRevitWall)
    {
      structural = speckleRevitWall.structural;
    }

    //if it's a new element, we don't need to update certain properties
    bool isUpdate = true;
    if (revitWall == null)
    {
      isUpdate = false;
      revitWall = DB.Wall.Create(Doc, baseCurve, wallType.Id, level.Id, 1, 0, false, structural);
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
      appObj.Update(status: ApplicationObject.State.Failed, logItem: "Creation returned null");
      return appObj;
    }

    //is structural update
    TrySetParam(revitWall, BuiltInParameter.WALL_STRUCTURAL_SIGNIFICANT, structural);

    if (isExactMatch && revitWall.WallType.Name != wallType.Name)
    {
      revitWall.ChangeTypeId(wallType.Id);
    }

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
        var topLevel = ConvertLevelToRevit(spklRevitWall.topLevel, out levelState);
        TrySetParam(revitWall, BuiltInParameter.WALL_HEIGHT_TYPE, topLevel);
      }
      else
      {
        TrySetParam(revitWall, BuiltInParameter.WALL_USER_HEIGHT_PARAM, speckleWall.height, speckleWall.units);
      }

      TrySetParam(revitWall, BuiltInParameter.WALL_BASE_OFFSET, spklRevitWall.baseOffset, speckleWall.units);
      TrySetParam(revitWall, BuiltInParameter.WALL_TOP_OFFSET, spklRevitWall.topOffset, speckleWall.units);
    }
    else
    {
      // Set wall unconnected height.
      TrySetParam(revitWall, BuiltInParameter.WALL_USER_HEIGHT_PARAM, speckleWall.height, speckleWall.units);

      TrySetParam(revitWall, BuiltInParameter.WALL_BASE_OFFSET, -baseOffset);
    }

    SetInstanceParameters(revitWall, speckleWall);

    var state = isUpdate ? ApplicationObject.State.Updated : ApplicationObject.State.Created;
    appObj.Update(status: state, createdId: revitWall.UniqueId, convertedItem: revitWall);

    SetWallVoids(revitWall, speckleWall);
    //appObj = SetHostedElements(speckleWall, revitWall, appObj);
    return appObj;
  }

  public Base WallToSpeckle(DB.Wall revitWall, out List<string> notes)
  {
    notes = new List<string>();
    var baseGeometry = LocationToSpeckle(revitWall);
    if (baseGeometry is Geometry.Point)
    {
      return RevitElementToSpeckle(revitWall, out notes);
    }

    RevitWall speckleWall =
      new(
        revitWall.WallType.FamilyName,
        revitWall.WallType.Name,
        (ICurve)baseGeometry,
        ConvertAndCacheLevel(revitWall, BuiltInParameter.WALL_BASE_CONSTRAINT),
        ConvertAndCacheLevel(revitWall, BuiltInParameter.WALL_HEIGHT_TYPE),
        GetParamValue<double>(revitWall, BuiltInParameter.WALL_USER_HEIGHT_PARAM),
        ModelUnits,
        revitWall.Id.ToString(),
        GetParamValue<double>(revitWall, BuiltInParameter.WALL_BASE_OFFSET),
        GetParamValue<double>(revitWall, BuiltInParameter.WALL_TOP_OFFSET),
        revitWall.Flipped,
        GetParamValue<bool>(revitWall, BuiltInParameter.WALL_STRUCTURAL_SIGNIFICANT)
      );
    //CreateVoids(revitWall, speckleWall);

    if (revitWall.CurtainGrid is not CurtainGrid grid)
    {
      if (revitWall.IsStackedWall)
      {
        var wallMembers = revitWall.GetStackedWallMemberIds().Select(id => (Wall)revitWall.Document.GetElement(id));
        speckleWall.elements = new List<Base>();
        foreach (var wall in wallMembers)
        {
          speckleWall.elements.Add(WallToSpeckle(wall, out List<string> stackedWallNotes));
        }
      }

      speckleWall.displayValue = GetElementDisplayValue(revitWall);
    }
    else
    {
      speckleWall.displayValue = new List<Mesh>(); // avoids null value for curtain walls
      AddHostedDependentElements(
        revitWall,
        speckleWall,
        GetSubsetOfElementsInView(BuiltInCategory.OST_CurtainWallMullions, grid.GetMullionIds()).ToList()
      );
      AddHostedDependentElements(
        revitWall,
        speckleWall,
        GetSubsetOfElementsInView(BuiltInCategory.OST_CurtainWallPanels, grid.GetPanelIds()).ToList()
      );
    }

    GetAllRevitParamsAndIds(
      speckleWall,
      revitWall,
      new List<string>
      {
        "WALL_USER_HEIGHT_PARAM",
        "WALL_BASE_OFFSET",
        "WALL_TOP_OFFSET",
        "WALL_BASE_CONSTRAINT",
        "WALL_HEIGHT_TYPE",
        "WALL_STRUCTURAL_SIGNIFICANT"
      }
    );

    GetWallVoids(speckleWall, revitWall);
    GetHostedElements(speckleWall, revitWall, out List<string> hostedNotes);
    if (hostedNotes.Any())
    {
      notes.AddRange(hostedNotes);
    }

    return speckleWall;
  }

  private IEnumerable<ElementId> GetSubsetOfElementsInView(BuiltInCategory category, IEnumerable<ElementId> children)
  {
    if (ViewSpecificOptions == null)
    {
      return children;
    }

    var allSubelementsInView = revitDocumentAggregateCache
      .GetOrInitializeEmptyCacheOfType<HashSet<ElementId>>(out _)
      .GetOrAdd(
        category.ToString(),
        () =>
        {
          using var filter = new ElementCategoryFilter(category);
          using var collector = new FilteredElementCollector(Doc, ViewSpecificOptions.View.Id);

          return new HashSet<ElementId>(collector.WhereElementIsNotElementType().WherePasses(filter).ToElementIds());
        },
        out _
      );

    return children.Where(allSubelementsInView.Contains);
  }

  //this is to prevent duplicated panels & mullions from being sent in curtain walls
  //might need improvement in the future
  //see https://github.com/specklesystems/speckle-sharp/issues/1197
  private List<ElementId> SubelementIds = new();

  private void GetWallVoids(Base speckleElement, Wall wall)
  {
#if !REVIT2020 && !REVIT2021
    var profile = ((Sketch)Doc.GetElement(wall.SketchId))?.Profile;

    if (profile == null)
    {
      return;
    }

    var voidsList = new List<OG.Polycurve>();
    for (var i = 1; i < profile.Size; i++)
    {
      var segments = CurveListToSpeckle(profile.get_Item(i).Cast<Curve>().ToList(), wall.Document);
      if (segments.segments.Count() > 2)
      {
        voidsList.Add(segments);
      }
    }
    if (voidsList.Count > 0)
    {
      speckleElement["voids"] = voidsList;
    }
#endif
  }

  private void SetWallVoids(Wall wall, Base speckleElement)
  {
#if !REVIT2020 && !REVIT2021
    if (speckleElement["voids"] == null || !(speckleElement["voids"] is IList voidCurves))
    {
      return;
    }

    if (wall.SketchId.IntegerValue == -1)
    {
      Doc.Regenerate();
      wall.CreateProfileSketch();
    }
    else
    {
      // TODO: actually update the profile in order to keep the user's dimensions
      wall.RemoveProfileSketch();
      wall.CreateProfileSketch();
    }

    // need to regen doc in order for the sketch to have an id
    Doc.Regenerate();
    var sketch = (Sketch)Doc.GetElement(wall.SketchId);

    transactionManager.Commit();
    var sketchEditScope = new SketchEditScope(Doc, "Add profile to the sketch");
    sketchEditScope.Start(sketch.Id);
    transactionManager.StartSubtransaction();

    foreach (var obj in voidCurves)
    {
      if (!(obj is ICurve @void))
      {
        continue;
      }

      var curveArray = CurveToNative(@void, true);
      Doc.Create.NewModelCurveArray(curveArray, sketch.SketchPlane);
    }
    if (transactionManager.Commit() != TransactionStatus.Committed)
    {
      sketchEditScope.Cancel();
    }

    try
    {
      sketchEditScope.Commit(new ErrorEater());
    }
    catch (Autodesk.Revit.Exceptions.InvalidOperationException)
    {
      if (sketchEditScope.IsActive)
      {
        sketchEditScope.Cancel();
      }
    }
    finally
    {
      transactionManager.StartSubtransaction();
    }
#endif
  }
}
