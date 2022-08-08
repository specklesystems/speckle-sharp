using Autodesk.Revit.DB;
using Speckle.Core.Models;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using DB = Autodesk.Revit.DB;
using View = Objects.BuiltElements.View;
using View3D = Objects.BuiltElements.View3D;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
    public View ViewToSpeckle(DB.View revitView)
    {
      switch (revitView.ViewType)
      {
        case ViewType.FloorPlan:
          break;
        case ViewType.CeilingPlan:
          break;
        case ViewType.Elevation:
          break;
        case ViewType.Section:
          break;
        case ViewType.ThreeD:
          break;
        default:
          break;
      }

      var speckleView = new View();

      if (revitView is DB.View3D rv3d)
      {
        // some views have null origin, not sure why, but for now we just ignore them and don't bother the user
        if (rv3d.Origin == null)
          return null;

        // get orientation
        var forward = rv3d.GetSavedOrientation().ForwardDirection; // this is unit vector
        var up = rv3d.GetSavedOrientation().UpDirection; // this is unit vector

        // get target
        var target = PointToSpeckle(CalculateTarget(rv3d, forward));

        speckleView = new View3D
        {
          origin = PointToSpeckle(rv3d.Origin),
          forwardDirection = VectorToSpeckle(forward, Speckle.Core.Kits.Units.None),
          upDirection = VectorToSpeckle(up, Speckle.Core.Kits.Units.None),
          target = target,
          isOrthogonal = !rv3d.IsPerspective,
          boundingBox = BoxToSpeckle(rv3d.CropBox),
          name = revitView.Name
        };

        // set props
        AttachViewParams(speckleView, rv3d);
      }

      GetAllRevitParamsAndIds(speckleView, revitView);
      Report.Log($"Converted View {revitView.ViewType} {revitView.Id}");
      return speckleView;
    }

    public DB.View ViewToNative(View3D speckleView)
    {
      var editViewName = EditViewName(speckleView.name, "SpeckleView");

      // get view3d type
      var viewType = new FilteredElementCollector(Doc)
        .WhereElementIsElementType()
        .OfClass(typeof(ViewFamilyType))
        .ToElements()
        .Cast<ViewFamilyType>()
        .FirstOrDefault(o => o.ViewFamily == ViewFamily.ThreeDimensional);

      // get orientation
      var up = new XYZ(speckleView.upDirection.x, speckleView.upDirection.y, speckleView.upDirection.z).Normalize(); //unit vector
      var forward = new XYZ(speckleView.forwardDirection.x, speckleView.forwardDirection.y, speckleView.forwardDirection.z).Normalize();
      if (Math.Round(up.DotProduct(forward), 3) != 0) // will throw error if vectors are not perpendicular
        return null;
      var orientation = new ViewOrientation3D(PointToNative(speckleView.origin), up, forward);

      // create view
      DB.View3D view = null;
      if (speckleView.isOrthogonal)
        view = DB.View3D.CreateIsometric(Doc, viewType.Id);
      else
        view = DB.View3D.CreatePerspective(Doc, viewType.Id);

      // set props
      view.SetOrientation(orientation);
      view.SaveOrientationAndLock();

      if (view.IsValidObject)
        SetInstanceParameters(view, speckleView);
      view = SetViewParams(view, speckleView);

      // set name last due to duplicate name errors
      try
      {
        view.Name = editViewName;
        return view;
      }
      catch (Exception e)
      {
        Report.ConversionErrors.Add(new Exception($@"View {editViewName} already exists."));
        view.Dispose();
        view = null;
        return null;
      }
    }

    private void AttachViewParams(View speckleView, DB.View view)
    {
      // display
      speckleView["display"] = view.DisplayStyle.ToString();

      // crop
      speckleView["cropped"] = view.CropBoxActive.ToString();
    }

    private DB.View3D SetViewParams(DB.View3D view, Base speckleView)
    {
      // display
      var display = speckleView["display"] as string;
      if (display != null)
      {
        var style = Enum.Parse(typeof(DisplayStyle), display);
        view.DisplayStyle = (style != null) ? (DisplayStyle)style : DisplayStyle.Wireframe;
      }

      // crop
      var crop = speckleView["cropped"] as string;
      if (crop != null)
        if (bool.TryParse(crop, out bool IsCropped))
          view.CropBoxActive = IsCropped;

      return view;
    }

    private XYZ CalculateTarget(DB.View3D view, XYZ forward)
    {
      var target = view.Origin.Add(forward * (view.CropBox.Max.Z - view.CropBox.Min.Z));
      var targetElevation = view.get_Parameter(BuiltInParameter.VIEWER_TARGET_ELEVATION).AsDouble();
      if (target.Z != targetElevation) // check if target matches stored elevation
      {
        double magnitude = (targetElevation - view.Origin.Z) / forward.Z;
        target = view.Origin.Add(forward * magnitude);
      }
      return target;
    }

    private string EditViewName(string name, string prefix = null)
    {
      var newName = name;
      // append commit info as prefix
      if (prefix != null)
        newName = prefix + "-" + name;

      // Check for invalid characters in view name
      var results = new Regex("[\\{\\}\\[\\]\\:|;<>?`~]")
        .Match(newName);

      // If none, fast exit
      if (results.Length <= 0)
        return newName;

      // Name contains invalid characters, replace accordingly.
      var corrected = Regex.Replace(newName, "[\\{\\[]", "(");
      corrected = Regex.Replace(corrected, "[\\}\\]]", ")");
      corrected = Regex.Replace(corrected, "[\\:|;<>?`~]", "-");

      Report.Log($@"Renamed view {name} to {corrected} due to invalid characters.");

      return corrected;
    }
  }
}