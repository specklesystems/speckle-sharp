using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Objects.BuiltElements;
using Objects.BuiltElements.Revit;
using Speckle.Core.Models;
using DB = Autodesk.Revit.DB;
using Vector = Objects.Geometry.Vector;

namespace Objects.Converter.Revit;

public partial class ConverterRevit
{
  // rebar
  /// <summary>
  /// Creates shape-driven rebar
  /// </summary>
  /// <param name="speckleRebar"></param>
  /// <returns></returns>
  public ApplicationObject RebarToNative(RevitRebarGroup speckleRebar)
  {
    var docObj = GetExistingElementByApplicationId(speckleRebar.applicationId);
    var appObj = new ApplicationObject(speckleRebar.id, speckleRebar.speckle_type)
    {
      applicationId = speckleRebar.applicationId
    };

    // skip if element already exists in doc & receive mode is set to ignore
    if (IsIgnore(docObj, appObj))
    {
      return appObj;
    }

    // return failed if rebar shape is null or has no curves
    if (speckleRebar.shape is not RevitRebarShape barShape || !barShape.curves.Any())
    {
      appObj.Update(status: ApplicationObject.State.Failed, logItem: "Rebar shape is null or has no curves.");
      return appObj;
    }

    // return failed if no valid host
    if (CurrentHostElement is null)
    {
      appObj.Update(status: ApplicationObject.State.Failed, logItem: "Host element was null.");
      return appObj;
    }

    // get rebar type and style
    var barType = GetElementType<RebarBarType>(speckleRebar, appObj, out bool isExactMatch);
    if (barType == null)
    {
      appObj.Update(status: ApplicationObject.State.Failed);
      return appObj;
    }

    if (speckleRebar.shape.rebarType == RebarType.Unknown)
    {
      appObj.Update(status: ApplicationObject.State.Failed, logItem: "Unknown bar type.");
      return appObj;
    }

    RebarStyle barStyle =
      speckleRebar.shape.rebarType == RebarType.Standard ? RebarStyle.Standard : RebarStyle.StirrupTie;

    // get start and end hooks and orientations
    RebarHookType startHook = null;
    RebarHookOrientation startHookOrientation = RebarHookOrientation.Right;
    if (speckleRebar.startHook is RevitRebarHook speckleStartHook)
    {
      startHook = RebarHookToNative(speckleStartHook);
      Enum.TryParse(speckleStartHook.orientation, out startHookOrientation);
    }

    RebarHookType endHook = null;
    RebarHookOrientation endHookOrientation = RebarHookOrientation.Right;
    if (speckleRebar.endHook is RevitRebarHook speckleEndHook)
    {
      endHook = RebarHookToNative(speckleEndHook);
      Enum.TryParse(speckleEndHook.orientation, out endHookOrientation);
    }

    // get the shape curves
    List<DB.Curve> curves = barShape.curves.SelectMany(o => CurveToNative(o).Cast<DB.Curve>()).ToList();
    if (curves is null || !curves.Any())
    {
      appObj.Update(status: ApplicationObject.State.Failed, logItem: "Could not convert any shape curves");
      return appObj;
    }

    // get the rebar plane norm from the curves
    XYZ normal = XYZ.BasisZ;
    if (speckleRebar.normal is not null)
    {
      normal = VectorToNative(speckleRebar.normal);
    }

    // create the rebar
    DB.Structure.Rebar rebar = null;
    try
    {
      rebar = DB.Structure.Rebar.CreateFromCurves(
        Doc,
        barStyle,
        barType,
        startHook,
        endHook,
        CurrentHostElement,
        normal,
        curves,
        startHookOrientation,
        endHookOrientation,
        true,
        true
      );
    }
    catch (Exception e)
    {
      appObj.Update(status: ApplicationObject.State.Failed, logItem: e.Message);
      return appObj;
    }

    // set layout rule after creation
    RebarShapeDrivenAccessor accessor = rebar.GetShapeDrivenAccessor();
    double arrayLength = ScaleToNative(speckleRebar.arrayLength, speckleRebar.units);
    double spacing = ScaleToNative(speckleRebar.spacing, speckleRebar.units);
    try
    {
      switch (speckleRebar.layoutRule)
      {
        case "FixedNumber":
          accessor.SetLayoutAsFixedNumber(
            speckleRebar.barPositions,
            arrayLength,
            speckleRebar.barsOnNormalSide,
            speckleRebar.hasFirstBar,
            speckleRebar.hasLastBar
          );
          break;
        case "MaximumSpacing":
          accessor.SetLayoutAsMaximumSpacing(
            spacing,
            arrayLength,
            speckleRebar.barsOnNormalSide,
            speckleRebar.hasFirstBar,
            speckleRebar.hasLastBar
          );
          break;
        case "MinimumClearSpacing":
          accessor.SetLayoutAsMinimumClearSpacing(
            spacing,
            arrayLength,
            speckleRebar.barsOnNormalSide,
            speckleRebar.hasFirstBar,
            speckleRebar.hasLastBar
          );
          break;
        case "NumberWithSpacing":
          accessor.SetLayoutAsNumberWithSpacing(
            speckleRebar.barPositions,
            spacing,
            speckleRebar.barsOnNormalSide,
            speckleRebar.hasFirstBar,
            speckleRebar.hasLastBar
          );
          break;
        case "Single":
          accessor.SetLayoutAsSingle();
          break;
        default:
          break;
      }
    }
    catch (Exception e)
    {
      appObj.Update(logItem: $"Could not set layout: {e.Message}");
    }

    SetInstanceParameters(rebar, speckleRebar);

    // deleting instead of updating for now!
    if (docObj != null)
    {
      Doc.Delete(docObj.Id);
    }

    appObj.Update(status: ApplicationObject.State.Created, createdId: rebar.UniqueId, convertedItem: rebar);

    return appObj;
  }

  /// <summary>
  /// Creates a Speckle RebarGroup for shape-driven revit rebar
  /// </summary>
  /// <param name="revitRebar"></param>
  /// <returns></returns>
  private RevitRebarGroup RebarToSpeckle(DB.Structure.Rebar revitRebar)
  {
    // skip freeform rebar for now: not supported by shape-driven RevitRebarGroup class
    // this is because freeform rebar with bent workshop can have multiple shapes and no accessor properties
    if (!revitRebar.IsRebarShapeDriven())
    {
      return null;
    }
    RebarShapeDrivenAccessor accessor = revitRebar.GetShapeDrivenAccessor();

    // get type
    var type = revitRebar.Document.GetElement(revitRebar.GetTypeId()) as ElementType;

    // get the rebar shape
    RevitRebarShape speckleShape = new();
    if (revitRebar.Document.GetElement(revitRebar.GetShapeId()) is DB.Structure.RebarShape revitShape)
    {
      speckleShape = RebarShapeToSpeckle(revitShape);
    }

#if REVIT2020 || REVIT2021
    speckleShape.barDiameter = revitRebar.GetBendData().BarDiameter;
#else
    speckleShape.barDiameter = revitRebar.GetBendData().BarModelDiameter;
#endif

    // get the rebar hooks
    DB.ElementId revitStartHookId = revitRebar.GetHookTypeId(0);
    RevitRebarHook speckleStartHook = null;
    double hookBendRadius = revitRebar.GetBendData().HookBendRadius;
    if (revitStartHookId != ElementId.InvalidElementId)
    {
      var revitStartHook = revitRebar.Document.GetElement(revitStartHookId) as RebarHookType;
      speckleStartHook = RebarHookToSpeckle(
        revitStartHook,
        revitRebar.GetHookOrientation(0).ToString(),
        hookBendRadius
      );
    }

    DB.ElementId revitEndHookId = revitRebar.GetHookTypeId(1);
    RevitRebarHook speckleEndHook = null;
    if (revitEndHookId != ElementId.InvalidElementId)
    {
      var revitEndHook = revitRebar.Document.GetElement(revitEndHookId) as RebarHookType;
      speckleEndHook = RebarHookToSpeckle(revitEndHook, revitRebar.GetHookOrientation(1).ToString(), hookBendRadius);
    }

    // get the layout rule - this determines exceptions that may be thrown by accessing invalid props
    bool isSingleLayout = revitRebar.LayoutRule == RebarLayoutRule.Single;

    // get centerline curves for display value
    // .GetCenterlineCurves() always returns the bar in the first position (even if it is excluded) for shape driven rebar groups
    IList<DB.Curve> firstPositionCurves = revitRebar.GetCenterlineCurves(
      true,
      false,
      false,
      MultiplanarOption.IncludeAllMultiplanarCurves,
      0
    );

    List<ICurve> centerlines = new();
    for (int i = 0; i < revitRebar.NumberOfBarPositions; i++)
    {
      // skip end bars that are excluded
      if (!isSingleLayout)
      {
        if (
          !revitRebar.IncludeFirstBar && i == 0
          || !revitRebar.IncludeLastBar && i == revitRebar.NumberOfBarPositions - 1
        )
        {
          continue;
        }
      }

      // for shape-driven rebar, get the transformed first position curves at this position
      var transform = accessor.GetBarPositionTransform(i);
      centerlines.AddRange(
        firstPositionCurves.Select(o => CurveToSpeckle(o.CreateTransformed(transform), revitRebar.Document)).ToList()
      );

      /* use for non-shape-driven rebar. compute the centerline at each position
      if (accessor is null)
      {
        IList<DB.Curve> revitCurves = revitRebar.GetCenterlineCurves(
          true,
          false,
          false,
          MultiplanarOption.IncludeAllMultiplanarCurves,
          i
        );
        centerlines.AddRange(revitCurves.Select(o => CurveToSpeckle(o, revitRebar.Document)).ToList());
      }
      */
    }

    // create speckle rebar
    RevitRebarGroup speckleRebar =
      new()
      {
        shape = speckleShape,
        number = revitRebar.Quantity,
        startHook = speckleStartHook,
        endHook = speckleEndHook,
        hasFirstBar = isSingleLayout || revitRebar.IncludeFirstBar,
        hasLastBar = isSingleLayout || revitRebar.IncludeLastBar,
        volume = revitRebar.Volume,
        family = type?.FamilyName,
        type = type?.Name,
        layoutRule = revitRebar.LayoutRule.ToString(),
        normal = VectorToSpeckle(accessor.Normal, revitRebar.Document),
        barsOnNormalSide = isSingleLayout || accessor.BarsOnNormalSide,
        arrayLength = isSingleLayout ? 0 : accessor.ArrayLength,
        barPositions = revitRebar.NumberOfBarPositions,
        spacing = GetParamValue<double>(revitRebar, BuiltInParameter.REBAR_ELEM_BAR_SPACING),
        displayValue = centerlines
      };

    // skip display value as meshes for now
    // GetElementDisplayValue(revitRebar, SolidDisplayValueOptions);
    GetAllRevitParamsAndIds(speckleRebar, revitRebar, new List<string> { "REBAR_ELEM_BAR_SPACING" });

    return speckleRebar;
  }

  // rebar shape
  private RevitRebarShape RebarShapeToSpeckle(DB.Structure.RebarShape revitRebarShape)
  {
    // get the type of the shape
    RebarType rebarType = RebarType.Unknown;
    switch (revitRebarShape.RebarStyle)
    {
      case RebarStyle.Standard:
        rebarType = RebarType.Standard;
        break;
      case RebarStyle.StirrupTie:
        rebarType = RebarType.StirrupPolygonal;
        break;
      default:
        break;
    }

    // get the curves representing the default values of the shape
    List<ICurve> curves = revitRebarShape
      .GetCurvesForBrowser()
      .Select(o => CurveToSpeckle(o, revitRebarShape.Document))
      .ToList();

    RevitRebarShape speckleRebarShape =
      new()
      {
        name = revitRebarShape.Name,
        curves = curves,
        rebarType = rebarType
      };

    GetAllRevitParamsAndIds(speckleRebarShape, revitRebarShape);

    return speckleRebarShape;
  }

  // rebar hook
  private RebarHookType RebarHookToNative(RevitRebarHook speckleRebarHook)
  {
    double multiplier = speckleRebarHook.multiplier > 0 ? speckleRebarHook.multiplier : 10; // default to 10 if invalid multiplier
    var revitRebarHook = RebarHookType.Create(Doc, speckleRebarHook.angle, multiplier);

    SetInstanceParameters(revitRebarHook, speckleRebarHook);

    return revitRebarHook;
  }

  private RevitRebarHook RebarHookToSpeckle(RebarHookType revitRebarHook, string orientation, double radius)
  {
    RevitRebarHook speckleRebarHook =
      new()
      {
        multiplier = revitRebarHook.StraightLineMultiplier,
        angle = revitRebarHook.HookAngle,
        orientation = orientation,
        radius = radius
      };

    GetAllRevitParamsAndIds(speckleRebarHook, revitRebarHook);

    return speckleRebarHook;
  }
}
