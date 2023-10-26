using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Objects.BuiltElements;
using Objects.BuiltElements.Revit;
using Speckle.Core.Models;
using DB = Autodesk.Revit.DB;

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

      // skip if rebar shape is null or has no curves
      var barShape = speckleRebar.shape as RevitRebarShape;
      if (barShape == null || !barShape.curves.Any())
      {
        appObj.Update(status: ApplicationObject.State.Failed, logItem: "Rebar shape is null or has no curves.");
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
      XYZ normal = null;
      try
      {
        using (CurveLoop loop = CurveLoop.Create(curves))
        {
          normal = loop.GetPlane().Normal;
        }
      }
      catch (ArgumentException e)
      {
        appObj.Update(status: ApplicationObject.State.Failed, logItem: $"{e.Message}");
        return appObj;
      }

      // create the rebar
      try
      {
        using (
          DB.Structure.Rebar rebar = DB.Structure.Rebar.CreateFromCurves(
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
          )
        )
        {
          // deleting instead of updating for now!
          if (docObj != null)
            Doc.Delete(docObj.Id);

          SetInstanceParameters(rebar, speckleRebar);

          appObj.Update(status: ApplicationObject.State.Created, createdId: rebar.UniqueId, convertedItem: rebar);
          return appObj;
        }
      }
      catch (Exception e)
      {
        appObj.Update(status: ApplicationObject.State.Failed, logItem: $"{e.Message}");
        return appObj;
      }
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

      // get the rebar hooks
      DB.ElementId revitStartHookId = revitRebar.GetHookTypeId(0);
      RevitRebarHook speckleStartHook = null;
      if (revitStartHookId != ElementId.InvalidElementId)
      {
        var revitStartHook = revitRebar.Document.GetElement(revitStartHookId) as RebarHookType;
        speckleStartHook = RebarHookToSpeckle(revitStartHook, revitRebar.GetHookOrientation(0).ToString());
      }
      DB.ElementId revitEndHookId = revitRebar.GetHookTypeId(1);
      RevitRebarHook speckleEndHook = null;
      if (revitEndHookId != ElementId.InvalidElementId)
      {
        var revitEndHook = revitRebar.Document.GetElement(revitEndHookId) as RebarHookType;
        speckleEndHook = RebarHookToSpeckle(revitEndHook, revitRebar.GetHookOrientation(1).ToString());
      }

      // get centerline curves
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

      var curves = new List<ICurve>();
      for (int i = 0; i < revitRebar.NumberOfBarPositions; i++)
      {
        // skip end bars that are excluded
        if (
          !revitRebar.IncludeFirstBar && i == 0
          || !revitRebar.IncludeLastBar && i == revitRebar.NumberOfBarPositions - 1
        )
        {
          continue;
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
          curves.AddRange(revitCurves.Select(o => CurveToSpeckle(o, revitRebar.Document)).ToList());
        }
        // for shape-driven rebar, get the transformed first position curves at this position
        else
        {
          var transform = accessor.GetBarPositionTransform(i);
          curves.AddRange(
            firstPositionCurves
              .Select(o => CurveToSpeckle(o.CreateTransformed(transform), revitRebar.Document))
              .ToList()
          );
        }
      }

      // create speckle rebar
      var speckleRebar = new RevitRebarGroup();
      speckleRebar.shape = speckleShape;
      speckleRebar.centerlines = curves;
      speckleRebar.startHook = speckleStartHook;
      speckleRebar.endHook = speckleEndHook;
      speckleRebar.number = revitRebar.Quantity;
      speckleRebar.hasFirstBar = revitRebar.IncludeFirstBar;
      speckleRebar.hasLastBar = revitRebar.IncludeLastBar;
      speckleRebar.volume = revitRebar.Volume;
      speckleRebar.family = type?.FamilyName;
      speckleRebar.type = type?.Name;

      // skip display value meshes for now
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

    private RevitRebarHook RebarHookToSpeckle(RebarHookType revitRebarHook, string orientation)
    {
      var speckleRebarHook = new RevitRebarHook();
      speckleRebarHook.angle = revitRebarHook.HookAngle;
      speckleRebarHook.orientation = orientation;
      return speckleRebarHook;
    }
  }
}
