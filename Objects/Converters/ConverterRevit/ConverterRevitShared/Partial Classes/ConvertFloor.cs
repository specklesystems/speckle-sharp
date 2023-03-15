using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Objects.BuiltElements.Revit;
using Objects.Geometry;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using DB = Autodesk.Revit.DB;
using OG = Objects.Geometry;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
    public ApplicationObject FloorToNative(BuiltElements.Floor speckleFloor)
    {
      var docObj = GetExistingElementByApplicationId(speckleFloor.applicationId);
      var appObj = new ApplicationObject(speckleFloor.id, speckleFloor.speckle_type) { applicationId = speckleFloor.applicationId };

      // skip if element already exists in doc & receive mode is set to ignore
      if (IsIgnore(docObj, appObj, out appObj))
        return appObj;

      if (speckleFloor.outline == null)
      {
        appObj.Update(status: ApplicationObject.State.Failed, logItem: "Floor is missing an outline.");
        return appObj;
      }

      bool structural = false;
      if (speckleFloor["structural"] is bool isStructural)
        structural = isStructural;

      DB.Level level;
      double slope = 0;
      DB.Line slopeDirection = null;
      if (speckleFloor is RevitFloor speckleRevitFloor)
      {
        level = ConvertLevelToRevit(speckleRevitFloor.level, out ApplicationObject.State state);
        structural = speckleRevitFloor.structural;
        slope = speckleRevitFloor.slope;
        slopeDirection = (speckleRevitFloor.slopeDirection != null) ? LineToNative(speckleRevitFloor.slopeDirection) : null;
      }
      else
      {
        level = ConvertLevelToRevit(LevelFromCurve(CurveToNative(speckleFloor.outline).get_Item(0)), out ApplicationObject.State state);
      }

      ChangeZValuesForCurves(speckleFloor.outline, level.Elevation);
      var outline = CurveToNative(speckleFloor.outline, true);
      UnboundCurveIfSingle(outline);

      if (!GetElementType<FloorType>(speckleFloor, appObj, out FloorType floorType))
      {
        appObj.Update(status: ApplicationObject.State.Failed);
        return appObj;
      }

      // NOTE: I have not found a way to edit a slab outline properly, so whenever we bake, we renew the element. The closest thing would be:
      // https://adndevbConversionLog.Add.typepad.com/aec/2013/10/change-the-boundary-of-floorsslabs.html
      // This would only work if the floors have the same number (and type!!!) of outline curves. 


      if (docObj != null)
        Doc.Delete(docObj.Id);

      DB.Floor revitFloor = null;
#if (REVIT2020 || REVIT2021)
      if (floorType == null)
      {
        if (slope != 0 && slopeDirection != null)
          revitFloor = Doc.Create.NewSlab(outline, level, slopeDirection, slope, structural);
        if (revitFloor == null)
          revitFloor = Doc.Create.NewFloor(outline, structural);
      }
      else
      {
        if (slope != 0 && slopeDirection != null)
          revitFloor = Doc.Create.NewSlab(outline, level, slopeDirection, slope, structural);
        if (revitFloor == null)
          revitFloor = Doc.Create.NewFloor(outline, floorType, level, structural);
      }

#else
      if (floorType == null)
        throw new SpeckleException("Floor needs a floor type");

      else
      {
        //from revit 2022 we can create openings in the floors!
        var profile = new List<CurveLoop> { CurveArrayToCurveLoop(outline) };
        if (speckleFloor["voids"] != null && (speckleFloor["voids"] is List<ICurve> voids))
        {
          foreach (var v in voids)
          {
            var opening = CurveArrayToCurveLoop(CurveToNative(v, true));
            profile.Add(opening);
          }
        }

        if (slope != 0 && slopeDirection != null)
          revitFloor = Floor.Create(Doc, profile, floorType.Id, level.Id, structural, slopeDirection, slope);
        if (revitFloor == null)
          revitFloor = Floor.Create(Doc, profile, floorType.Id, level.Id);
      }
#endif

      Doc.Regenerate();

#if (REVIT2020 || REVIT2021)
      try
      {
        CreateVoids(revitFloor, speckleFloor);
      }
      catch (Exception ex)
      {
        appObj.Update(logItem: $"Could not create openings: {ex.Message}");
      }
#endif

      SetInstanceParameters(revitFloor, speckleFloor);

      appObj.Update(status: ApplicationObject.State.Created, createdId: revitFloor.UniqueId, convertedItem: revitFloor);
      appObj = SetHostedElements(speckleFloor, revitFloor, appObj);
      return appObj;
    }

    private RevitFloor FloorToSpeckle(DB.Floor revitFloor, out List<string> notes)
    {
      notes = new List<string>();
      var profiles = GetProfiles(revitFloor);

      var speckleFloor = new RevitFloor();
      var type = revitFloor.Document.GetElement(revitFloor.GetTypeId()) as ElementType;
      speckleFloor.family = type?.FamilyName;
      speckleFloor.type = type?.Name;
      speckleFloor.outline = profiles[0];
      if (profiles.Count > 1)
      {
        speckleFloor.voids = profiles.Skip(1).ToList();
      }

      speckleFloor.level = ConvertAndCacheLevel(revitFloor, BuiltInParameter.LEVEL_PARAM);
      speckleFloor.structural = GetParamValue<bool>(revitFloor, BuiltInParameter.FLOOR_PARAM_IS_STRUCTURAL);

      // Divide by 100 to convert from percentage to unitless ratio (rise over run)
      var slopeParam = GetParamValue<double?>(revitFloor, BuiltInParameter.ROOF_SLOPE) / 100;

      GetAllRevitParamsAndIds(speckleFloor, revitFloor, new List<string> { "LEVEL_PARAM", "FLOOR_PARAM_IS_STRUCTURAL", "ROOF_SLOPE" });

      GetSlopeArrowHack(revitFloor.Id, revitFloor.Document, out var tail, out var head, out double tailOffset, out double headOffset, out double slope);

      slopeParam ??= slope;
      speckleFloor.slope = (double)slopeParam;

      if (tail != null && head != null)
      {
        speckleFloor.slopeDirection = new Geometry.Line(tail, head);
        if (speckleFloor["parameters"] is Base parameters && parameters["FLOOR_HEIGHTABOVELEVEL_PARAM"] is BuiltElements.Revit.Parameter offsetParam && offsetParam.value is double offset)
        {
          offsetParam.value = offset + tailOffset;
          parameters["FLOOR_HEIGHTABOVELEVEL_PARAM"] = offsetParam;
        }
      }

      speckleFloor.displayValue = GetElementDisplayMesh(revitFloor, new Options() { DetailLevel = ViewDetailLevel.Fine, ComputeReferences = false });

      GetHostedElements(speckleFloor, revitFloor, out List<string> hostedNotes);
      if (hostedNotes.Any()) notes.AddRange(hostedNotes);
      return speckleFloor;
    }

    // Nesting the various profiles into a polycurve segments. 
    // TODO: **These should be HORIZONTAL on the floor level!** otherwise sloped floors will not be converted back to native properly
    private List<ICurve> GetProfiles(DB.CeilingAndFloor floor)
    {
      var profiles = new List<ICurve>();
      var faces = HostObjectUtils.GetTopFaces(floor);
      Face face = floor.GetGeometryObjectFromReference(faces[0]) as Face;
      var crvLoops = face.GetEdgesAsCurveLoops();
      foreach (var crvloop in crvLoops)
      {
        var poly = new Polycurve(ModelUnits);
        foreach (var curve in crvloop)
        {
          var c = curve;
          if (c == null)
            continue;

          poly.segments.Add(CurveToSpeckle(c, floor.Document));
        }
        profiles.Add(poly);
      }
      return profiles;
    }

    // in order to create a revit floor, we need to pass it a flat profile so change all the z values
    private void ChangeZValuesForCurves(ICurve curve, double z)
    {
      switch (curve)
      {
        case OG.Line line:
          // kind of a hack. Editing our speckle objects is so much easier than editing the revit objects
          // so scale this z value from the model units to the incoming units and then they will get scaled
          // back to the model units when this entire outline gets scaled to native
          line.start.z = z * Speckle.Core.Kits.Units.GetConversionFactor(ModelUnits, line.start.units);
          line.end.z = z * Speckle.Core.Kits.Units.GetConversionFactor(ModelUnits, line.end.units);
          return;

        //case OG.Arc arc:
        //case OG.Circle circle:
        //case OG.Ellipse ellipse:
        //case OG.Spiral spiral:

        case OG.Curve nurbs:
          for (int i = 2; i < nurbs.points.Count; i += 3)
            nurbs.points[i] = z * Speckle.Core.Kits.Units.GetConversionFactor(ModelUnits, nurbs.units);
          return;

        case OG.Polyline poly:
          foreach (var point in poly.GetPoints())
            point.z = z * Speckle.Core.Kits.Units.GetConversionFactor(ModelUnits, point.units);
          return;

        case OG.Polycurve plc:
          foreach (var seg in plc.segments)
            ChangeZValuesForCurves(seg, z);
          return;
      }
    }
  }
}
