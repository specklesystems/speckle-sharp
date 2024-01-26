#if GRASSHOPPER
#endif
using System;
using System.Collections.Generic;
using System.Linq;
using Objects.BuiltElements;
using Objects.Geometry;
using Objects.Other;
using Rhino;
using Rhino.Display;
using Rhino.DocObjects;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using RH = Rhino.Geometry;
using RV = Objects.BuiltElements.Revit;

namespace Objects.Converter.RhinoGh;

public partial class ConverterRhinoGh
{
  // parameters
  public Tuple<string, string> ParameterToNative(RV.Parameter parameter)
  {
    var name = parameter.name;
    var val = parameter.value?.ToString() ?? string.Empty;
    return new Tuple<string, string>(name, val);
  }

  // views
  public View3D ViewToSpeckle(ViewInfo view)
  {
    // get orientation vectors
    var up = view.Viewport.CameraUp;
    var forward = view.Viewport.CameraDirection;
    up.Unitize();
    forward.Unitize();

    var _view = new View3D();
    _view.name = view.Name;
    _view.upDirection = new Vector(up.X, up.Y, up.Z, "none");
    _view.forwardDirection = new Vector(forward.X, forward.Y, forward.Z, "none");
    _view.origin = PointToSpeckle(view.Viewport.CameraLocation);
    _view.target = PointToSpeckle(view.Viewport.TargetPoint);
    _view.isOrthogonal = view.Viewport.IsParallelProjection ? true : false;
    _view.units = ModelUnits;

    // get view bounding box
    var near = view.Viewport.GetNearPlaneCorners();
    var far = view.Viewport.GetFarPlaneCorners();
    if (near.Length > 0 && far.Length > 0)
    {
      var box = new RH.Box(new RH.BoundingBox(near[0], far[3]));
      _view.boundingBox = BoxToSpeckle(box);
    }

    // attach props
    AttachViewParams(_view, view);

    return _view;
  }

  public ApplicationObject ViewToNative(View3D view)
  {
    var appObj = new ApplicationObject(view.id, view.speckle_type) { applicationId = view.applicationId };

    var bakedViewName = string.Empty;
    RhinoApp.InvokeOnUiThread(
      (Action)
        delegate
        {
          RhinoView _view = Doc.Views.ActiveView;
          RhinoViewport viewport = _view.ActiveViewport;
          viewport.SetProjection(DefinedViewportProjection.Perspective, null, false);
          var origin = PointToNative(view.origin).Location;
          var forward = new RH.Vector3d(view.forwardDirection.x, view.forwardDirection.y, view.forwardDirection.z);

          if (view.target != null)
          {
            viewport.SetCameraLocations(PointToNative(view.target).Location, origin); // this changes viewport.CameraUp. works for axon from revit if after, for perspective from revit if before
          }
          else
          {
            viewport.SetCameraLocation(origin, true);
            viewport.SetCameraDirection(forward, true);
          }
          viewport.CameraUp = new RH.Vector3d(view.upDirection.x, view.upDirection.y, view.upDirection.z);

          viewport.Name = view.name;

          /* TODO: debug this and see if it helps better match views from revit
          // set bounding box
          var box = BoxToNative(view.boundingBox);
          BoundingBox boundingBox = new BoundingBox(box.X.Min, box.Y.Min, box.Z.Min, box.X.Max, box.Y.Max, box.Z.Max);
          viewport.SetClippingPlanes(boundingBox);
          */

          // set rhino view props if available
          SetViewParams(viewport, view);

          if (view.isOrthogonal)
          {
            viewport.ChangeToParallelProjection(true);
          }

          var commitInfo = GetCommitInfo();
          bakedViewName = ReceiveMode == ReceiveMode.Create ? $"{commitInfo} - {view.name}" : $"{view.name}";

          var res = Doc.NamedViews.Add(bakedViewName, viewport.Id);
          if (res == -1)
          {
            appObj.Update(status: ApplicationObject.State.Failed, logItem: "Could not add named view to doc");
          }
          else
          {
            var namedView = Doc.NamedViews[res];
            appObj.Update(bakedViewName, convertedItem: namedView);
          }
        }
    );

    return appObj;
  }

  private void AttachViewParams(Base speckleView, ViewInfo view)
  {
    // lens
    speckleView["lens"] = view.Viewport.Camera35mmLensLength;

    // frustrum
    if (
      view.Viewport.GetFrustum(
        out double left,
        out double right,
        out double bottom,
        out double top,
        out double near,
        out double far
      )
    )
    {
      speckleView["frustrum"] = new List<double> { left, right, bottom, top, near, far };
    }

    // crop
    speckleView["cropped"] = bool.FalseString;
  }

  private RhinoViewport SetViewParams(RhinoViewport viewport, Base speckleView)
  {
    // lens
    var lens = speckleView["lens"] as double?;
    if (lens != null)
    {
      viewport.Camera35mmLensLength = (double)lens;
    }

    return viewport;
  }

  // level
  public ApplicationObject LevelToNative(Level level)
  {
    var appObj = new ApplicationObject(level.id, level.speckle_type) { applicationId = level.applicationId };

    var commitInfo = GetCommitInfo();
    var bakedLevelName = ReceiveMode == ReceiveMode.Create ? $"{commitInfo} - {level.name}" : $"{level.name}";

    var elevation = ScaleToNative(level.elevation, level.units);
    var plane = new RH.Plane(new RH.Point3d(0, 0, elevation), RH.Vector3d.ZAxis);
    var res = Doc.NamedConstructionPlanes.Add(bakedLevelName, plane);

    if (res == -1)
    {
      appObj.Update(status: ApplicationObject.State.Failed, logItem: "Could not add named construction plane to doc");
    }
    else
    {
      var namedCPlane = Doc.NamedConstructionPlanes[res];
      appObj.Update(bakedLevelName, convertedItem: namedCPlane);
    }

    return appObj;
  }

  // gridline
  public ApplicationObject GridlineToNative(GridLine gridline)
  {
    var appObj = new ApplicationObject(gridline.id, gridline.speckle_type) { applicationId = gridline.applicationId };

    // create the curve
    var curve = CurveToNative(gridline.baseLine);
    if (curve == null)
    {
      appObj.Update(status: ApplicationObject.State.Failed, logItem: "Could not convert curve");
      return appObj;
    }
    // get linetype
    ObjectAttributes atts = null;
    if (gridline["@displayStyle"] as DisplayStyle == null)
    {
      var linetypeIndex = Doc.Linetypes.Find("Dashed");
      if (linetypeIndex >= 0)
      {
        atts = new ObjectAttributes()
        {
          LinetypeIndex = linetypeIndex,
          LinetypeSource = ObjectLinetypeSource.LinetypeFromObject
        };
      }
    }

    // bake the curve
    Guid id = atts != null ? Doc.Objects.Add(curve, atts) : Doc.Objects.Add(curve);
    if (id == Guid.Empty)
    {
      appObj.Update(status: ApplicationObject.State.Failed, logItem: "Could not add curve to doc");
      return appObj;
    }
    var _gridLine = Doc.Objects.FindId(id);
    appObj.Update(convertedItem: _gridLine, createdId: id.ToString());

    // create and bake two textdots at the endpoints of the curve
    if (!string.IsNullOrEmpty(gridline.label))
    {
      var labelStartId = Doc.Objects.AddTextDot(gridline.label, curve.PointAtStart);
      if (labelStartId != Guid.Empty)
      {
        appObj.Update(convertedItem: Doc.Objects.FindId(labelStartId), createdId: labelStartId.ToString());
      }

      var labelEndId = Doc.Objects.AddTextDot(gridline.label, curve.PointAtEnd);
      if (labelEndId != Guid.Empty)
      {
        appObj.Update(convertedItem: Doc.Objects.FindId(labelEndId), createdId: labelEndId.ToString());
      }
    }

    return appObj;
  }

  #region CIVIL

  // alignment
  public RH.Curve AlignmentToNative(Alignment alignment)
  {
    var curves = new List<RH.Curve>();
    foreach (var entity in alignment.curves)
    {
      var converted = CurveToNative(entity);
      if (converted != null)
      {
        curves.Add(converted);
      }
    }
    if (curves.Count == 0)
    {
      return null;
    }

    // try to join entity curves
    var joined = RH.Curve.JoinCurves(curves);
    return joined.First();
  }

  #endregion
}
