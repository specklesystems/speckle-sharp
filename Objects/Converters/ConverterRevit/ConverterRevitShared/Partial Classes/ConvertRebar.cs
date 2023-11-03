using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.Exceptions;
using Objects.BuiltElements;
using Objects.BuiltElements.Revit;
using Speckle.Core.Models;
using DB = Autodesk.Revit.DB;
using Vector = Objects.Geometry.Vector;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
    // rebar
    public ApplicationObject RebarToNative(RevitRebarGroup speckleRebar)
    {
      var docObj = GetExistingElementByApplicationId(speckleRebar.applicationId);
      var appObj = new ApplicationObject(speckleRebar.id, speckleRebar.speckle_type)
      {
        applicationId = speckleRebar.applicationId
      };

      // skip if element already exists in doc & receive mode is set to ignore
      if (IsIgnore(docObj, appObj))
        return appObj;

      // return failed if rebar shape is null or has no curves
      var barShape = speckleRebar.shape as RevitRebarShape;
      if (barShape == null || !barShape.curves.Any())
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
      var barStyle =
        speckleRebar.shape.rebarType != RebarType.Unknown && speckleRebar.shape.rebarType == RebarType.Standard
          ? RebarStyle.Standard
          : RebarStyle.StirrupTie;

      // get start and end hooks and orientations
      var speckleStartHook = speckleRebar.startHook as RevitRebarHook;
      RebarHookType startHook = null;
      RebarHookOrientation startHookOrientation = RebarHookOrientation.Right;
      if (speckleStartHook != null)
      {
        startHook = RebarHookToNative(speckleStartHook);
        Enum.TryParse(speckleStartHook.orientation, out startHookOrientation);
      }
      var speckleEndHook = speckleRebar.endHook as RevitRebarHook;
      RebarHookType endHook = null;
      RebarHookOrientation endHookOrientation = RebarHookOrientation.Right;
      if (speckleEndHook != null)
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
      catch (Autodesk.Revit.Exceptions.ArgumentNullException nullEx)
      {
        appObj.Update(status: ApplicationObject.State.Failed, logItem: nullEx.Message);
      }
      catch (Autodesk.Revit.Exceptions.ArgumentOutOfRangeException rangeEx)
      {
        appObj.Update(status: ApplicationObject.State.Failed, logItem: rangeEx.Message);
      }
      catch (Autodesk.Revit.Exceptions.ArgumentsInconsistentException inconsistentEx)
      {
        appObj.Update(status: ApplicationObject.State.Failed, logItem: inconsistentEx.Message);
      }
      catch (Autodesk.Revit.Exceptions.ArgumentException argException)
      {
        var info =
          "The element host is not a valid rebar host. -or- The input curves "
          + "contains at least one curve which is not a bound Line or bound Arc and is not "
          + "supported for this operation. -or- curves do not form a valid CurveLoop. -or- The "
          + "input curves contains at least one helical curve and is not supported for this operation.";
        appObj.Update(status: ApplicationObject.State.Failed, logItem: $"{argException.Message}: {info}");
      }
      catch (Autodesk.Revit.Exceptions.DisabledDisciplineException disciplineException)
      {
        appObj.Update(status: ApplicationObject.State.Failed, logItem: disciplineException.Message);
      }
      finally
      {
        if (rebar != null)
        {
          SetInstanceParameters(rebar, speckleRebar);

          // set additional params
          if (speckleRebar.barPositions > 0)
          {
            rebar.NumberOfBarPositions = speckleRebar.barPositions;
          }

          // deleting instead of updating for now!
          if (docObj != null)
            Doc.Delete(docObj.Id);

          appObj.Update(status: ApplicationObject.State.Created, createdId: rebar.UniqueId, convertedItem: rebar);
        }
      }
      return appObj;
    }

    private RevitRebarGroup RebarToSpeckle(DB.Structure.Rebar revitRebar)
    {
      // skip freeform rebar for now: not supported by RevitRebarGroup class
      // this is because freeform rebar with bent workshop has multiple shapes
      if (revitRebar.GetAllRebarShapeIds().Count > 1)
      {
        return null;
      }

      // get type
      var type = revitRebar.Document.GetElement(revitRebar.GetTypeId()) as ElementType;

      // get the rebar shape
      var revitShape = revitRebar.Document.GetElement(revitRebar.GetShapeId()) as DB.Structure.RebarShape;
      RevitRebarShape speckleShape = RebarShapeToSpeckle(revitShape);
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
      RebarShapeDrivenAccessor accessor = null;
      if (revitRebar.IsRebarShapeDriven())
        accessor = revitRebar.GetShapeDrivenAccessor();
      // .GetCenterlineCurves() always returns the bar in the first position (even if it is excluded) for shape driven rebar groups
      IList<DB.Curve> firstPositionCurves = revitRebar.GetCenterlineCurves(
        true,
        false,
        false,
        MultiplanarOption.IncludeAllMultiplanarCurves,
        0
      );

      var centerlines = new List<ICurve>();
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

        // for non-shape-driven rebar, compute the centerline at each position
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
        // for shape-driven rebar, get the transformed first position curves at this position
        else
        {
          var transform = accessor.GetBarPositionTransform(i);
          centerlines.AddRange(
            firstPositionCurves
              .Select(o => CurveToSpeckle(o.CreateTransformed(transform), revitRebar.Document))
              .ToList()
          );
        }
      }

      // get plane normal of rebar group
      // the normal prop was deprecated in revit 2018, and the accessor normal is suggested as a replacement
      // unclear how non-shape-driven rebar set normals are retrieved or computed.
      Vector normal = null;
      if (accessor != null)
      {
        normal = VectorToSpeckle(accessor.Normal, revitRebar.Document);
      }

      // create speckle rebar
      var speckleRebar = new RevitRebarGroup();
      speckleRebar.shape = speckleShape;
      speckleRebar.startHook = speckleStartHook;
      speckleRebar.endHook = speckleEndHook;
      speckleRebar.number = revitRebar.Quantity;
      speckleRebar.hasFirstBar = isSingleLayout ? true : revitRebar.IncludeFirstBar;
      speckleRebar.hasLastBar = isSingleLayout ? true : revitRebar.IncludeLastBar;
      speckleRebar.volume = revitRebar.Volume;
      speckleRebar.family = type?.FamilyName;
      speckleRebar.type = type?.Name;
      speckleRebar.normal = normal;
      speckleRebar.barPositions = revitRebar.NumberOfBarPositions;
      speckleRebar.displayValue = centerlines;

      // skip display value as meshes for now
      // GetElementDisplayValue(revitRebar, SolidDisplayValueOptions);
      GetAllRevitParamsAndIds(speckleRebar, revitRebar);
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
      }

      // get the curves representing the default values of the shape
      List<ICurve> curves = revitRebarShape
        .GetCurvesForBrowser()
        .Select(o => CurveToSpeckle(o, revitRebarShape.Document))
        .ToList();

      var speckleRebarShape = new RevitRebarShape();
      speckleRebarShape.name = revitRebarShape.Name;
      speckleRebarShape.curves = curves;
      speckleRebarShape.rebarType = rebarType;

      GetAllRevitParamsAndIds(speckleRebarShape, revitRebarShape);
      return speckleRebarShape;
    }

    // rebar hook
    private RebarHookType RebarHookToNative(RevitRebarHook speckleRebarHook)
    {
      var revitRebarHook = RebarHookType.Create(Doc, speckleRebarHook.angle, 10); // using default multiplier, will be set by parameters later
      SetInstanceParameters(revitRebarHook, speckleRebarHook);

      return revitRebarHook;
    }

    private RevitRebarHook RebarHookToSpeckle(RebarHookType revitRebarHook, string orientation, double radius)
    {
      var speckleRebarHook = new RevitRebarHook();
      speckleRebarHook.angle = revitRebarHook.HookAngle;
      speckleRebarHook.orientation = orientation;
      speckleRebarHook.radius = radius;
      GetAllRevitParamsAndIds(speckleRebarHook, revitRebarHook);
      return speckleRebarHook;
    }
  }
}
