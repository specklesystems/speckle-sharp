using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Autodesk.Revit.DB;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using DB = Autodesk.Revit.DB;
using View = Objects.BuiltElements.View;
using View3D = Objects.BuiltElements.View3D;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
    private List<string> excludedParameters = new List<string>()
    {
      "VIEW_NAME", // param value is already stored in name prop of view and setting this param can cause errors 
    };
    public Base ViewToSpeckle(DB.View revitView)
    {
      switch (revitView)
      {
        case DB.View3D o:
          return View3DToSpeckle(o);
        case DB.ViewSchedule o:
          return ScheduleToSpeckle(o);
        default:
          var speckleView = new View();
          GetAllRevitParamsAndIds(speckleView, revitView, excludedParameters);
          Report.Log($"Converted View {revitView.ViewType} {revitView.Id}");
          return speckleView;
      }
    }
    public View View3DToSpeckle(DB.View3D revitView)
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
          throw new ConversionSkippedException($"Views with no origin are not supported");

        // get orientation
        var forward = rv3d.GetSavedOrientation().ForwardDirection; // this is unit vector
        var up = rv3d.GetSavedOrientation().UpDirection; // this is unit vector

        // get target
        var target = PointToSpeckle(CalculateTarget(rv3d, forward), revitView.Document);

        speckleView = new View3D
        {
          origin = PointToSpeckle(rv3d.Origin, revitView.Document),
          forwardDirection = VectorToSpeckle(forward, revitView.Document, Speckle.Core.Kits.Units.None),
          upDirection = VectorToSpeckle(up, revitView.Document, Speckle.Core.Kits.Units.None),
          target = target,
          isOrthogonal = !rv3d.IsPerspective,
          boundingBox = BoxToSpeckle(rv3d.CropBox, revitView.Document),
          name = revitView.Name
        };

        // set props
        AttachViewParams(speckleView, rv3d);
      }

      GetAllRevitParamsAndIds(speckleView, revitView, excludedParameters);
      Report.Log($"Converted View {revitView.ViewType} {revitView.Id}");
      return speckleView;
    }

    public ApplicationObject ViewToNative(View3D speckleView)
    {
      var appObj = new ApplicationObject(speckleView.id, speckleView.speckle_type) { applicationId = speckleView.applicationId };

      DB.View3D view = null;
      var viewNameSplit = speckleView.name.Split('-');

      // get orientation
      var up = new XYZ(speckleView.upDirection.x, speckleView.upDirection.y, speckleView.upDirection.z).Normalize(); //unit vector
      var forward = new XYZ(speckleView.forwardDirection.x, speckleView.forwardDirection.y, speckleView.forwardDirection.z).Normalize();
      if (Math.Round(up.DotProduct(forward), 3) != 0) // will throw error if vectors are not perpendicular
      {
        appObj.Update(status: ApplicationObject.State.Failed, logItem: "The up and forward vectors for this view are not perpendicular.");
        return appObj;
      }
      var orientation = new ViewOrientation3D(PointToNative(speckleView.origin), up, forward);

      var editViewName = EditViewName(speckleView.name, "SpeckleView");
      // get the existing view with this name, if there is one
      view = new FilteredElementCollector(Doc)
          .WhereElementIsNotElementType()
          .OfClass(typeof(DB.View3D))
          .Cast<DB.View3D>()
          .FirstOrDefault(o => o.Name == editViewName);

      if (view == null)
      {
        // get view3d type
        var viewType = new FilteredElementCollector(Doc)
          .WhereElementIsElementType()
          .OfClass(typeof(ViewFamilyType))
          .ToElements()
          .Cast<ViewFamilyType>()
          .FirstOrDefault(o => o.ViewFamily == ViewFamily.ThreeDimensional);

        // create view
        if (speckleView.isOrthogonal)
          view = DB.View3D.CreateIsometric(Doc, viewType.Id);
        else
          view = DB.View3D.CreatePerspective(Doc, viewType.Id);

        view.Name = editViewName;
        appObj.Update(status: ApplicationObject.State.Created);
      }
      else if (view.IsLocked)
      {
        appObj.Update(status: ApplicationObject.State.Failed, logItem: $"View named {editViewName} is locked and cannot be modified.");
        return appObj;
      }
      else
        appObj.Update(status: ApplicationObject.State.Updated);

      // set props
      view.SetOrientation(orientation);
      view.SaveOrientationAndLock();

      if (view.IsValidObject)
        SetInstanceParameters(view, speckleView, excludedParameters);
      view = SetViewParams(view, speckleView);

      appObj.Update(createdId: view.UniqueId, convertedItem: view);
      return appObj;
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
      var viewNameSplit = name.Split('-');
      // if the name already starts with the prefix, don't add the prefix again
      if (viewNameSplit.Length > 0 && viewNameSplit.First() == prefix)
        return name;

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

    /// <summary>
    /// Converts a Speckle comment camera coordinates into Revit's
    /// First three values are the camera's position, second three the target
    /// </summary>
    /// <param name="speckleCamera"></param>
    /// <returns></returns>
    public DB.ViewOrientation3D ViewOrientation3DToNative(Base baseCamera)
    {
      //hacky but the current comments camera is not a Base object
      var speckleCamera = baseCamera["coordinates"] as List<double>;
      var position = new Objects.Geometry.Point(speckleCamera[0], speckleCamera[1], speckleCamera[2]);
      var target = new Objects.Geometry.Point(speckleCamera[3], speckleCamera[4], speckleCamera[5]);


      var cameraTarget = PointToNative(target);
      var cameraPosition = PointToNative(position);
      var cameraDirection = (cameraTarget.Subtract(cameraPosition)).Normalize();
      var cameraUpVector = cameraDirection.CrossProduct(XYZ.BasisZ).CrossProduct(cameraDirection);


      return new ViewOrientation3D(cameraPosition, cameraUpVector, cameraDirection);
    }
  }
}
