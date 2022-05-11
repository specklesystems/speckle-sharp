using Autodesk.Revit.DB.Structure;
using Objects.BuiltElements.Revit;
using Speckle.Core.Models;
using System.Collections.Generic;
using System.Linq;
using DB = Autodesk.Revit.DB;
using Polycurve = Objects.Geometry.Polycurve;
using Polyline = Objects.Geometry.Polyline;
using Rebar = Objects.BuiltElements.Rebar;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
    public List<ApplicationPlaceholderObject> RebarToNative(Rebar speckleRebar)
    {
      if (speckleRebar.curves.Count == 0)
        throw new Speckle.Core.Logging.SpeckleException("Rebar has no base curves.");

      var speckleRevitRebar = speckleRebar as RevitRebar;
      if (speckleRevitRebar != null)
        throw new Speckle.Core.Logging.SpeckleException("Rebar needs to be a Revit rebar.");

      var rebarType = speckleRevitRebar?.barType;
      var barType = GetElementType<RebarBarType>(speckleRebar);
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
        throw new Speckle.Core.Logging.SpeckleException("Rebar host not found.");

      var docObj = GetExistingElementByApplicationId(speckleRebar.applicationId);
      if (docObj != null && ReceiveMode == Speckle.Core.Kits.ReceiveMode.Ignore)
        return new List<ApplicationPlaceholderObject>
      {
        new ApplicationPlaceholderObject
          {applicationId = speckleRebar.applicationId, ApplicationGeneratedId = docObj.UniqueId, NativeObject = docObj}
      };
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
      {
        rebar.MoveBarInSet(0, DB.Transform.Identity);
      }
#endif

      // create freeform from curveloops if they exist
      if (curveLoops.Count > 0)
      {
        var result = RebarFreeFormValidationResult.Success;
        rebar ??= DB.Structure.Rebar.CreateFreeForm(Doc, barType, host, curveLoops, out result);
        if (result != RebarFreeFormValidationResult.Success)
          throw new Speckle.Core.Logging.SpeckleException("Freeform Rebar could not be created from closed curves.");
      }
      else if (openCurves.Count > 0)
      {
        //rebar ??= DB.Structure.Rebar.CreateFromCurves(Doc, barType, host, curveLoops, out result);
      }
      else
        throw new Speckle.Core.Logging.SpeckleException("No convertible rebar curves where found.");

      if (speckleRevitRebar != null)
        SetInstanceParameters(rebar, speckleRevitRebar);

      var placeholders = new List<ApplicationPlaceholderObject>
      {
        new ApplicationPlaceholderObject
          {applicationId = speckleRebar.applicationId, ApplicationGeneratedId = rebar.UniqueId, NativeObject = rebar}
      };
      Report.Log($"Created Rebar {rebar.Id}");
      return placeholders;
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
        curves.Add(CurveToSpeckle(bar));
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