using Grasshopper.Kernel.Types;
using Objects.Geometry;
using Objects.Primitive;
using Rhino.Geometry;
using Rhino.Display;
using Rhino.DocObjects;
using Rhino.Geometry.Collections;
using Speckle.Core.Models;
using Speckle.Core.Kits;
using System;
using System.Collections.Generic;
using System.Linq;
using Alignment = Objects.BuiltElements.Alignment; 
using Column = Objects.BuiltElements.Column;
using Beam = Objects.BuiltElements.Beam;
using Duct = Objects.BuiltElements.Duct;
using Wall = Objects.BuiltElements.Wall;
using Floor = Objects.BuiltElements.Floor;
using Ceiling = Objects.BuiltElements.Ceiling;
using Pipe = Objects.BuiltElements.Pipe;
using Roof = Objects.BuiltElements.Roof;
using Opening = Objects.BuiltElements.Opening;
using Point = Objects.Geometry.Point;
using View3D = Objects.BuiltElements.View3D;
using RH = Rhino.Geometry;
using RV = Objects.BuiltElements.Revit;

namespace Objects.Converter.RhinoGh
{
  public partial class ConverterRhinoGh
  {
    public View3D ViewToSpeckle(ViewInfo view)
    {
      // get orientation vectors
      var up = view.Viewport.CameraUp;
      var forward = view.Viewport.CameraDirection;
      up.Unitize(); forward.Unitize();

      var _view = new View3D();
      _view.name = view.Name;
      _view.upDirection = new Vector(up.X, up.Y, up.Z, "none");
      _view.forwardDirection = new Vector(forward.X, forward.Y, forward.Z, "none"); 
      _view.origin = PointToSpeckle(view.Viewport.CameraLocation);
      _view.target = PointToSpeckle(view.Viewport.TargetPoint);
      _view.isOrthogonal = (view.Viewport.IsParallelProjection) ? true : false;
      _view.units = ModelUnits;

      // get view bounding box
      var near = view.Viewport.GetNearPlaneCorners();
      var far = view.Viewport.GetFarPlaneCorners();
      if (near.Length > 0 && far.Length > 0)
      {
        var box = new RH.Box(new BoundingBox(near[0], far[3]));
        _view.boundingBox = BoxToSpeckle(box);
      }

      // attach props
      AttachViewParams(_view, view);

      return _view;
    }
    public string ViewToNative(View3D view)
    {
      Rhino.RhinoApp.InvokeOnUiThread((Action)delegate {

        RhinoView _view = Doc.Views.ActiveView;
        RhinoViewport viewport = _view.ActiveViewport;
        viewport.SetProjection(DefinedViewportProjection.Perspective, null, false);
        var origin = PointToNative(view.origin).Location;
        var forward = new Vector3d(view.forwardDirection.x, view.forwardDirection.y, view.forwardDirection.z);

        if (view.target != null)
        {
          viewport.SetCameraLocations(PointToNative(view.target).Location, origin); // this changes viewport.CameraUp. works for axon from revit if after, for perspective from revit if before
        }
        else
        {
          viewport.SetCameraLocation(origin, true);
          viewport.SetCameraDirection(forward, true);
        }
        viewport.CameraUp = new Vector3d(view.upDirection.x, view.upDirection.y, view.upDirection.z);

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
          viewport.ChangeToParallelProjection(true);

        var commitInfo = GetCommitInfo();
        var viewName = $"{commitInfo } - {view.name}";

        Doc.NamedViews.Add(viewName, viewport.Id);
      });

      //ConversionErrors.Add(sdfasdfaf);

      return "baked";
    }

    private void AttachViewParams(Base speckleView, ViewInfo view)
    {
      // lens
      speckleView["lens"] = view.Viewport.Camera35mmLensLength;

      // frustrum
      if (view.Viewport.GetFrustum(out double left, out double right, out double bottom, out double top, out double near, out double far))
        speckleView["frustrum"] = new List<double>() { left, right, bottom, top, near, far };

      // crop
      speckleView["cropped"] = bool.FalseString;
    }
    private RhinoViewport SetViewParams(RhinoViewport viewport, Base speckleView)
    {
      // lens
      var lens = speckleView["lens"] as double?;
      if (lens != null)
        viewport.Camera35mmLensLength = (double)lens;

      return viewport;
    }

    public Column CurveToSpeckleColumn(RH.Curve curve)
    {
      return new Column(CurveToSpeckle(curve)) { units = ModelUnits };
    }

    public Beam CurveToSpeckleBeam(RH.Curve curve)
    {
      return new Beam(CurveToSpeckle(curve)) { units = ModelUnits };
    }

    // args of format [width, height, diameter]
    public Duct CurveToSpeckleDuct(RH.Curve curve, string[] args)
    {
      Duct duct = null;
      if (args.Length < 3) return duct;
      if (double.TryParse(args[0], out double height) && double.TryParse(args[1], out double width) && double.TryParse(args[2], out double diameter))
        duct = new Duct(CurveToSpeckle(curve), width, height, diameter) { units = ModelUnits, length = curve.GetLength() };
      return duct;
    }

    // args of format [diameter]
    public Pipe CurveToSpecklePipe(RH.Curve curve, string[] args)
    {
      Pipe pipe = null;
      if (args.Length < 1) return pipe;
      if (double.TryParse(args[0], out double diameter))
        pipe = new Pipe(CurveToSpeckle(curve), curve.GetLength(), diameter) { units = ModelUnits };
      return pipe;
    }

    public Opening CurveToSpeckleOpening(RH.Curve curve)
    {
      return new Opening((ICurve)ConvertToSpeckle(curve)) { units = ModelUnits };
    }

    public Floor CurveToSpeckleFloor(RH.Curve curve)
    {
      return new Floor((ICurve)ConvertToSpeckle(curve)) { units = ModelUnits };
    }

    public Wall BrepToSpeckleWall(RH.Brep brep)
    {
      Wall wall = null;
      BoundingBox brepBox = brep.GetBoundingBox(false);
      double height = brepBox.Max.Z - brepBox.Min.Z; // extract height
      var bottomCurves = GetSurfaceBrepEdges(brep, getBottom: true); // extract baseline
      var intCurves = GetSurfaceBrepEdges(brep, getInterior: true); // extract openings
      List<Base> openings = new List<Base>();
      if (intCurves != null)
        foreach (ICurve crv in intCurves)
          openings.Add(new Opening(crv));
      if (bottomCurves != null && height > 0)
        wall = new Wall(height, bottomCurves[0], openings) { units = ModelUnits };
      return wall;
    }

    public Floor BrepToSpeckleFloor(RH.Brep brep)
    {
      Floor floor = null;
      var extCurves = GetSurfaceBrepEdges(brep, getExterior: true); // extract outline
      var intCurves = GetSurfaceBrepEdges(brep, getInterior: true); // extract voids
      if (extCurves != null)
        floor = new Floor(extCurves[0], intCurves) { units = ModelUnits };
      return floor;
    }

    public Roof BrepToSpeckleRoof(RH.Brep brep)
    {
      Roof roof = null;
      var extCurves = GetSurfaceBrepEdges(brep, getExterior: true); // extract outline
      var intCurves = GetSurfaceBrepEdges(brep, getInterior: true); // extract voids
      if (extCurves != null)
        roof = new Roof(extCurves[0], intCurves) { units = ModelUnits };
      return roof;
    }

    public RV.RevitFaceWall BrepToFaceWall(RH.Brep brep, string[] args)
    {
      if (brep.Faces.Count > 1)
        return null;
      
      string family = "Default";
      string type = "Default";
      try { family = args[0]; type = args[1]; } catch { }
      return new RV.RevitFaceWall(family, type, BrepToSpeckle(brep), null) { units = ModelUnits };
    }

    public RV.AdaptiveComponent InstanceToAdaptiveComponent(InstanceObject instance, string[] args)
    {
      if (args.Length == 0)
        return null;

      string family = "Default";
      string type = "Default";
      try { family = args[0]; type = args[1]; } catch { }

      var points = instance.GetSubObjects().Select(o => PointToSpeckle(((Rhino.Geometry.Point)o.Geometry).Location)).ToList();

      var adaptiveComponent = new RV.AdaptiveComponent(type, family, points);
      adaptiveComponent.units = ModelUnits;

      return adaptiveComponent;
    }

    public RV.DirectShape BrepToDirectShape(RH.Brep brep, string[] args)
    {
      if (args.Length == 0)
        return null;
      if (!Enum.TryParse($"{args[0]}s", out RV.RevitCategory category))
        return null;
      string name = "DirectShape";
      try { name = args[1]; } catch { }
      return new RV.DirectShape(name, category, new List<Base>() { ConvertToSpeckle(brep) }) { units = ModelUnits };
    }

    public RV.DirectShape ExtrusionToDirectShape(RH.Extrusion extrusion, string[] args)
    {
      if (args.Length == 0)
        return null;
      if (!Enum.TryParse($"{args[0]}s", out RV.RevitCategory category))
        return null;
      string name = "DirectShape";
      try { name = args[1]; } catch { }
      return new RV.DirectShape(name, category, new List<Base>() { ConvertToSpeckle(extrusion) }) { units = ModelUnits };
    }

    public RV.DirectShape MeshToDirectShape(RH.Mesh mesh, string[] args)
    {
      if (args.Length == 0)
        return null;
      if (!Enum.TryParse($"{args[0]}s", out RV.RevitCategory category))
        return null;
      string name = "DirectShape";
      try { name = args[1]; } catch { }
      return new RV.DirectShape(name, category, new List<Base>() { ConvertToSpeckle(mesh) }) { units = ModelUnits };
    }

    // edge curve convenience method
    private List<ICurve> GetSurfaceBrepEdges(RH.Brep brep, bool getExterior = true, bool getInterior = false, bool getBottom = false)
    {
      double tol = Doc.ModelAbsoluteTolerance * 1;

      RH.Curve[] brpCurves = null;
      if (getInterior)
        brpCurves = brep.DuplicateNakedEdgeCurves(false, true);
      else
        brpCurves = brep.DuplicateNakedEdgeCurves(true, false);
      if (getBottom)
      {
        var bottomCrv = brpCurves.
          Where(o => o.IsLinear())?.
          Where(o => new Vector3d(o.PointAtEnd.X - o.PointAtStart.X, o.PointAtEnd.Y - o.PointAtStart.Y, o.PointAtEnd.Z - o.PointAtStart.Z).IsPerpendicularTo(Vector3d.ZAxis))?.
          Aggregate((curMin, o) => curMin == null || o.PointAtStart.Z < curMin.PointAtStart.Z ? o : curMin);
        if (bottomCrv != null)
          brpCurves = new RH.Curve[] { bottomCrv };
      }

      List<ICurve> outCurves = null ;
      if (brpCurves != null && brpCurves.Count() > 0)
        outCurves = (brpCurves.Count() == 1) ? new List<ICurve>() { (ICurve)ConvertToSpeckle(brpCurves[0]) } : RH.Curve.JoinCurves(brpCurves, tol).Select(o => (ICurve)ConvertToSpeckle(o)).ToList();
      return outCurves;
    }
    
    public List<object> DirectShapeToNative(RV.DirectShape directShape)
    {
      if (directShape.displayValue == null)
      {
        Report.Log($"Skipping DirectShape {directShape.id} because it has no {nameof(directShape.displayValue)}");
        return null;
      }

      if (directShape.displayValue.Count == 0)
      {
        Report.Log($"Skipping DirectShape {directShape.id} because {nameof(directShape.displayValue)} was empty");
        return null;
      }

      IEnumerable<object> subObjects = directShape.displayValue.Select(ConvertToNative)
        .Where(e => e != null);
          
      var nativeObjects = subObjects.ToList(); 
      
      if (nativeObjects.Count == 0)
      {
        Report.Log($"Skipping DirectShape {directShape.id} because {nameof(directShape.displayValue)} contained no convertable elements");
        return null;
      }

      return nativeObjects;
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
          curves.Add(converted);
      }
      if (curves.Count == 0) return null;

      // try to join entity curves
      var joined = RH.Curve.JoinCurves(curves);
      return joined.First();
    }

    #endregion
  }
}