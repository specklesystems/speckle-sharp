using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB.Structure;
using Objects.BuiltElements.Revit;
using Speckle.Core.Models;
using DB = Autodesk.Revit.DB;
using Polycurve = Objects.Geometry.Polycurve;
using Polyline = Objects.Geometry.Polyline;
using Rebar = Objects.BuiltElements.Rebar;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
    public ApplicationObject RebarToNative(Rebar speckleRebar)
    {
      var docObj = GetExistingElementByApplicationId(speckleRebar.applicationId);
      var appObj = new ApplicationObject(speckleRebar.id, speckleRebar.speckle_type) { applicationId = speckleRebar.applicationId };

      // skip if element already exists in doc & receive mode is set to ignore
      if (IsIgnore(docObj, appObj))
        return appObj;

      if (speckleRebar.curves.Count == 0)
      {
        appObj.Update(status: ApplicationObject.State.Failed, logItem: "Rebar has no base curves.");
        return appObj;
      }

      var speckleRevitRebar = speckleRebar as RevitRebar;
      if (speckleRevitRebar == null)
      {
        appObj.Update(status: ApplicationObject.State.Failed, logItem: "Rebar needs to be a Revit rebar.");
        return appObj;
      }

      var rebarType = speckleRevitRebar?.barType;
      var barType = GetElementType<RebarBarType>(speckleRebar, appObj, out bool _);
      if (barType == null)
      {
        appObj.Update(status: ApplicationObject.State.Failed);
        return appObj;
      }
      var rebarStyle = speckleRevitRebar?.barStyle == "StirrupTie" ? RebarStyle.StirrupTie : RebarStyle.Standard;

      // get construction curves (only works for revit rebar due to need for host)
      var closedCurves = new List<DB.CurveArray>();
      var openCurves = new List<DB.Curve>();
      ProcessRebarCurves(speckleRebar, out openCurves, out closedCurves);

      // find some way to test if curves should be converted to curve loops for freeform rebar or not for rebar from curves
      var curveLoops = closedCurves.Select(o => CurveArrayToCurveLoop(o)).ToList();

      DB.Structure.Rebar rebar = null;

      // get host element
      var host = GetExistingElementByApplicationId(speckleRevitRebar.host);
      if (host == null)
      {
        appObj.Update(status: ApplicationObject.State.Failed, logItem: "Rebar host not found.");
        return appObj;
      }

      if (docObj != null)
      {
        rebar = (DB.Structure.Rebar)docObj;
        // if the number of curves don't match, we need to create a new bar
        if (rebar.Quantity != closedCurves.Count + openCurves.Count)
          rebar = null;
      }

      // update curves if we can, only available in revit 2022
#if REVIT2022
      if (rebar != null)
        rebar.MoveBarInSet(0, DB.Transform.Identity);
#endif

      // create freeform from curveloops if they exist
      if (curveLoops.Count > 0)
      {
        var result = RebarFreeFormValidationResult.Success;
        rebar ??= DB.Structure.Rebar.CreateFreeForm(Doc, barType, host, curveLoops, out result);
        if (result != RebarFreeFormValidationResult.Success)
        {
          appObj.Update(status: ApplicationObject.State.Failed, logItem: "Freeform Rebar could not be created from closed curves.");
          return appObj;
        }
      }
      else if (openCurves.Count > 0)
      {
        //rebar ??= DB.Structure.Rebar.CreateFromCurves(Doc, barType, host, curveLoops, out result);
      }
      else
      {
        appObj.Update(status: ApplicationObject.State.Failed, logItem: "No convertible rebar curves where found.");
        return appObj;
      }

      if (speckleRevitRebar != null)
        SetInstanceParameters(rebar, speckleRevitRebar);

      appObj.Update(status: ApplicationObject.State.Created, createdId: rebar.UniqueId, convertedItem: rebar);
      return appObj;
    }

    // need to test to see possible types of curves for rebar
    private void ProcessRebarCurves(Rebar rebar, out List<DB.Curve> open, out List<DB.CurveArray> closed)
    {
      var _open = new List<DB.Curve>();
      var _closed = new List<DB.CurveArray>();

      foreach (var curve in rebar.curves)
      {
        var array = CurveToNative(curve);
        switch (curve)
        {
          case Polyline o:
            if (o.closed)
              _closed.Add(array);
            else
            {
              var iterator = array.ForwardIterator();
              while (iterator.MoveNext())
                _open.Add(iterator.Current as DB.Curve);
            }
            break;
          case Polycurve o:
            if (o.closed)
              _closed.Add(array);
            else
            {
              var iterator = array.ForwardIterator();
              while (iterator.MoveNext())
                _open.Add(iterator.Current as DB.Curve);
            }
            break;
          default:
            _open.Add(array.get_Item(0));
            break;
        }
      }

      open = _open; closed = _closed;
    }

    private Base RebarToSpeckle(DB.Structure.Rebar revitRebar)
    {
      // get rebar centerline curves using transform
      var bars = revitRebar.GetCenterlineCurves(true, true, true, MultiplanarOption.IncludeOnlyPlanarCurves, revitRebar.NumberOfBarPositions - 1);
      var curves = new List<ICurve>();

      RebarShapeDrivenAccessor accessor = null;
      if (revitRebar.IsRebarShapeDriven())
        accessor = revitRebar.GetShapeDrivenAccessor();

      for (int i = 0; i < bars.Count; i++)
      {
        var bar = (accessor != null) ? bars[i].CreateTransformed(accessor.GetBarPositionTransform(i)) : bars[i];
        curves.Add(CurveToSpeckle(bar, revitRebar.Document));
      }

      var speckleRebar = new RevitRebar();
      speckleRebar.host = revitRebar.GetHostId().ToString();
      speckleRebar.type = revitRebar.Document.GetElement(revitRebar.GetTypeId()).Name;
      speckleRebar.curves = curves;
      speckleRebar.shapes = revitRebar.GetAllRebarShapeIds().Select(o => o.ToString()).ToList(); // freeform rebar with bent workshop has multiple shapes
      speckleRebar.volume = revitRebar.Volume;

      GetAllRevitParamsAndIds(speckleRebar, revitRebar);

      return speckleRebar;
    }

  }
}
