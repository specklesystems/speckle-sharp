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
using OO = Objects.Other;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
    public ApplicationObject FloorToNative(BuiltElements.Floor speckleFloor)
    {
      var docObj = GetExistingElementByApplicationId(speckleFloor.applicationId);
      var appObj = new ApplicationObject(speckleFloor.id, speckleFloor.speckle_type)
      {
        applicationId = speckleFloor.applicationId
      };

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
        slopeDirection =
          (speckleRevitFloor.slopeDirection != null) ? LineToNative(speckleRevitFloor.slopeDirection) : null;
      }
      else
      {
        level = ConvertLevelToRevit(
          LevelFromCurve(CurveToNative(speckleFloor.outline).get_Item(0)),
          out ApplicationObject.State state
        );
      }

      var flattenedOutline = GetFlattenedCurve(speckleFloor.outline, level.Elevation);
      var outline = CurveToNative(flattenedOutline, true);
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

      GetAllRevitParamsAndIds(
        speckleFloor,
        revitFloor,
        new List<string> { "LEVEL_PARAM", "FLOOR_PARAM_IS_STRUCTURAL", "ROOF_SLOPE" }
      );

      //DISABLED BECAUSE OF MAJOR PERFORMANCE IMPACT
      //GetSlopeArrowHack(
      //  revitFloor.Id,
      //  revitFloor.Document,
      //  out var tail,
      //  out var head,
      //  out double tailOffset,
      //  out double headOffset,
      //  out double slope
      //);

      //slopeParam ??= slope;
      //speckleFloor.slope = (double)slopeParam;

      //if (tail != null && head != null)
      //{
      //  speckleFloor.slopeDirection = new Geometry.Line(tail, head);
      //  if (
      //    speckleFloor["parameters"] is Base parameters
      //    && parameters["FLOOR_HEIGHTABOVELEVEL_PARAM"] is BuiltElements.Revit.Parameter offsetParam
      //    && offsetParam.value is double offset
      //  )
      //  {
      //    offsetParam.value = offset + tailOffset;
      //    parameters["FLOOR_HEIGHTABOVELEVEL_PARAM"] = offsetParam;
      //  }
      //}

      speckleFloor.displayValue = GetElementDisplayMesh(
        revitFloor,
        new Options() { DetailLevel = ViewDetailLevel.Fine, ComputeReferences = false }
      );

      GetHostedElements(speckleFloor, revitFloor, out List<string> hostedNotes);
      if (hostedNotes.Any())
        notes.AddRange(hostedNotes);
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
    private ICurve GetFlattenedCurve(ICurve curve, double z)
    {
      // kind of a hack. Editing our speckle objects is so much easier than editing the revit objects
      // so scale this z value from the model units to the incoming commit units and then those values will
      // get scaled back to the model units when this entire outline gets scaled to native

      switch (curve)
      {
        case OG.Line line:
          return new OG.Line(
            new OG.Point(
              line.start.x,
              line.start.y,
              z * Speckle.Core.Kits.Units.GetConversionFactor(ModelUnits, line.start.units),
              line.start.units
            ),
            new OG.Point(
              line.end.x,
              line.end.y,
              z * Speckle.Core.Kits.Units.GetConversionFactor(ModelUnits, line.end.units),
              line.end.units
            ),
            line.units
          );

        case OG.Arc arc:
          var normalUnit = arc.plane.normal.Unit();
          var normalAsPoint = new OG.Point(normalUnit.x, normalUnit.y, normalUnit.z);
          var arcConversionFactor = Speckle.Core.Kits.Units.GetConversionFactor(ModelUnits, arc.units);

          if (normalAsPoint.DistanceTo(new OG.Point(0, 0, 1)) < TOLERANCE)
          {
            var translation = new OG.Vector(0, 0, (z * arcConversionFactor) - arc.startPoint.z) { units = ModelUnits };
            var transform = new OO.Transform(
              new OG.Vector(1, 0, 0),
              new OG.Vector(0, 1, 0),
              new OG.Vector(0, 0, 1),
              translation
            );
            _ = arc.TransformTo(transform, out OG.Arc newArc);
            return newArc;
          }
          else
          {
            // sneakily replace the users arc with a curve 🤫.
            // TODO: set knots in curve or apply more complex transforms to arc because this replacement is decent but not perfect

            var newPlane = new OG.Plane(
              new OG.Point(arc.plane.origin.x, arc.plane.origin.y, arc.plane.origin.z),
              arc.plane.normal,
              arc.plane.xdir,
              arc.plane.ydir
            );
            var firstHalfArc = new OG.Arc(
              newPlane,
              arc.midPoint,
              arc.startPoint,
              arc.angleRadians / 2,
              units: arc.units
            );
            var secondHalfArc = new OG.Arc(
              newPlane,
              arc.endPoint,
              arc.midPoint,
              arc.angleRadians / 2,
              units: arc.units
            );
            var arcCurvePoints = new List<double>
            {
              arc.startPoint.x,
              arc.startPoint.y,
              z * arcConversionFactor,
              firstHalfArc.midPoint.x,
              firstHalfArc.midPoint.y,
              z * arcConversionFactor,
              arc.midPoint.x,
              arc.midPoint.y,
              z * arcConversionFactor,
              secondHalfArc.midPoint.x,
              secondHalfArc.midPoint.y,
              z * arcConversionFactor,
              arc.endPoint.x,
              arc.endPoint.y,
              z * arcConversionFactor
            };

            var newArcCurve = new OG.Curve
            {
              points = arcCurvePoints,
              weights = new List<double>(Enumerable.Repeat(1.0, arcCurvePoints.Count)),
              //knots = nurbs.knots,
              //degree = nurbs.degree,
              rational = false,
              closed = false,
              //newCurve.domain
              //newCurve.length
              units = arc.units
            };

            return newArcCurve;
          }

        //case OG.Circle circle:
        //case OG.Ellipse ellipse:
        //case OG.Spiral spiral:

        case OG.Curve nurbs:
          var curvePoints = new List<double>();
          var nurbsConversionFactor = Speckle.Core.Kits.Units.GetConversionFactor(ModelUnits, nurbs.units);
          for (var i = 0; i < nurbs.points.Count; i++)
          {
            if (i % 3 == 2)
              curvePoints.Add(z * nurbsConversionFactor);
            else
              curvePoints.Add(nurbs.points[i]);
          }
          var newCurve = new OG.Curve
          {
            points = curvePoints,
            weights = nurbs.weights,
            knots = nurbs.knots,
            degree = nurbs.degree,
            rational = nurbs.rational,
            closed = nurbs.closed,
            //newCurve.domain
            //newCurve.length
            units = nurbs.units
          };
          return newCurve;

        case OG.Polyline poly:
          var polylinePonts = new List<double>();
          var originalPolylinePoints = poly.GetPoints();
          var polyConversionFactor = Speckle.Core.Kits.Units.GetConversionFactor(ModelUnits, poly.units);
          for (var i = 0; i < originalPolylinePoints.Count; i++)
          {
            polylinePonts.Add(originalPolylinePoints[i].x);
            polylinePonts.Add(originalPolylinePoints[i].y);
            polylinePonts.Add(z * polyConversionFactor);
          }
          var newPolyline = new Polyline(polylinePonts, poly.units);
          newPolyline.closed = poly.closed;
          return newPolyline;

        case OG.Polycurve plc:
          var newPolycurve = new Polycurve(plc.units);
          foreach (var seg in plc.segments)
            newPolycurve.segments.Add(GetFlattenedCurve(seg, z));
          return newPolycurve;
      }
      throw new NotSupportedException($"Trying to flatten unsupported curve type, {curve.GetType()}");
    }
  }
}
