using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Objects.BuiltElements;
using Objects.BuiltElements.Revit.RevitRoof;
using Objects.Geometry;
using RevitSharedResources.Extensions.SpeckleExtensions;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using DB = Autodesk.Revit.DB;
using FamilyInstance = Objects.BuiltElements.Revit.FamilyInstance;
using Line = Objects.Geometry.Line;

namespace Objects.Converter.Revit;

public partial class ConverterRevit
{
  public ApplicationObject RoofToNative(Roof speckleRoof)
  {
    Element docObj = GetExistingElementByApplicationId((speckleRoof).applicationId);
    var appObj = new ApplicationObject(speckleRoof.id, speckleRoof.speckle_type)
    {
      applicationId = speckleRoof.applicationId
    };

    // skip if element already exists in doc & receive mode is set to ignore
    if (IsIgnore(docObj, appObj))
    {
      return appObj;
    }

    // outline is required for footprint roofs
    // referenceLine is required for for Extrusion roofs
    CurveArray roofCurve = null;
    if (speckleRoof is RevitExtrusionRoof extrusionRoof)
    {
      if (extrusionRoof.referenceLine is null)
      {
        appObj.Update(status: ApplicationObject.State.Failed, logItem: "Extrusion roof profile was null");
        return appObj;
      }
      roofCurve = CurveToNative(extrusionRoof.referenceLine);
    }
    else
    {
      if (speckleRoof.outline is null)
      {
        appObj.Update(status: ApplicationObject.State.Failed, logItem: "Roof outline was null");
        return appObj;
      }
      roofCurve = CurveToNative(speckleRoof.outline);
    }

    // retrieve the level
    var levelState = ApplicationObject.State.Unknown;
    double baseOffset = 0.0;
    DB.Level level = speckleRoof.level is not null
      ? ConvertLevelToRevit(speckleRoof.level, out levelState)
      : roofCurve is not null
        ? ConvertLevelToRevit(roofCurve.get_Item(0), out levelState, out baseOffset)
        : null;

    var speckleRevitRoof = speckleRoof as RevitRoof;

    var roofType = GetElementType<RoofType>(speckleRoof, appObj, out bool _);
    if (roofType == null)
    {
      appObj.Update(status: ApplicationObject.State.Failed);
      return appObj;
    }

    if (docObj != null)
    {
      Doc.Delete(docObj.Id);
    }

    DB.RoofBase revitRoof = null;
    switch (speckleRoof)
    {
      case RevitExtrusionRoof speckleExtrusionRoof:
      {
        // get the norm
        var referenceLine = LineToNative(speckleExtrusionRoof.referenceLine);
        var norm = GetPerpendicular(referenceLine.GetEndPoint(0) - referenceLine.GetEndPoint(1)).Negate();
        ReferencePlane plane = Doc.Create.NewReferencePlane(
          referenceLine.GetEndPoint(0),
          referenceLine.GetEndPoint(1),
          norm,
          Doc.ActiveView
        );

        //create floor without a type with the profile
        var start = ScaleToNative(speckleExtrusionRoof.start, speckleExtrusionRoof.units);
        var end = ScaleToNative(speckleExtrusionRoof.end, speckleExtrusionRoof.units);
        revitRoof = Doc.Create.NewExtrusionRoof(roofCurve, plane, level, roofType, start, end);

        // sometimes Revit flips the roof so the start offset is the end and vice versa.
        // In that case, delete the created roof, flip the referencePlane and recreate it.
        var actualStart = GetParamValue<double>(revitRoof, BuiltInParameter.EXTRUSION_START_PARAM);
        if (actualStart - speckleExtrusionRoof.end < TOLERANCE)
        {
          Doc.Delete(revitRoof.Id);
          plane.Flip();
          revitRoof = Doc.Create.NewExtrusionRoof(roofCurve, plane, level, roofType, start, end);
        }
        break;
      }
      case RevitFootprintRoof speckleFootprintRoof:
      {
        ModelCurveArray curveArray = new();
        var revitFootprintRoof = Doc.Create.NewFootPrintRoof(roofCurve, level, roofType, out curveArray);

        // if the roof is a curtain roof then set the mullions at the borders
        var nestedElements = speckleFootprintRoof.elements;
        if (revitFootprintRoof.CurtainGrids != null && nestedElements is not null && nestedElements.Count != 0)
        {
          // TODO: Create a new type instead of overriding the type. This could affect other elements
          var param = roofType.get_Parameter(BuiltInParameter.AUTO_MULLION_BORDER1_GRID1);
          var type = Doc.GetElement(param.AsElementId());
          if (type == null)
          {
            // assuming first mullion is the desired mullion for the whole roof...
            var mullionType = GetElementType<MullionType>(
              nestedElements.First(b => b is FamilyInstance f),
              appObj,
              out bool _
            );
            if (mullionType != null)
            {
              TrySetParam(roofType, BuiltInParameter.AUTO_MULLION_BORDER1_GRID1, mullionType);
              TrySetParam(roofType, BuiltInParameter.AUTO_MULLION_BORDER1_GRID2, mullionType);
              TrySetParam(roofType, BuiltInParameter.AUTO_MULLION_BORDER2_GRID1, mullionType);
              TrySetParam(roofType, BuiltInParameter.AUTO_MULLION_BORDER2_GRID2, mullionType);
            }
          }
        }
        var poly = speckleFootprintRoof.outline as Polycurve;
        bool hasSlopedSide = false;
        if (poly != null)
        {
          for (var i = 0; i < curveArray.Size; i++)
          {
            var isSloped = ((Base)poly.segments[i])["isSloped"] as bool?;
            var slopeAngle = ((Base)poly.segments[i])["slopeAngle"] as double?;
            var offset = ((Base)poly.segments[i])["offset"] as double?;

            if (isSloped != null)
            {
              revitFootprintRoof.set_DefinesSlope(curveArray.get_Item(i), isSloped == true);
              if (slopeAngle != null && isSloped == true)
              {
                // slope is set using actual slope (rise / run) for this method
                revitFootprintRoof.set_SlopeAngle(curveArray.get_Item(i), Math.Tan((double)slopeAngle * Math.PI / 180));
                hasSlopedSide = true;
              }
            }

            if (offset != null)
            {
              revitFootprintRoof.set_Offset(
                curveArray.get_Item(i),
                ScaleToNative((double)offset, speckleFootprintRoof.units)
              );
            }
          }
        }

        //this is for schema builder specifically
        //if no roof edge has a slope defined but a slope angle is defined on the roof
        //set each edge to have that slope
        if (!hasSlopedSide && speckleFootprintRoof.slope != null && speckleFootprintRoof.slope != 0)
        {
          for (var i = 0; i < curveArray.Size; i++)
          {
            revitFootprintRoof.set_DefinesSlope(curveArray.get_Item(i), true);
          }

          TrySetParam(revitFootprintRoof, BuiltInParameter.ROOF_SLOPE, (double)speckleFootprintRoof.slope);
        }

        if (speckleFootprintRoof.cutOffLevel != null)
        {
          var cutOffLevel = ConvertLevelToRevit(speckleFootprintRoof.cutOffLevel, out levelState);
          TrySetParam(revitFootprintRoof, BuiltInParameter.ROOF_UPTO_LEVEL_PARAM, cutOffLevel);
        }

        revitRoof = revitFootprintRoof;
        break;
      }
      default:
        appObj.Update(
          status: ApplicationObject.State.Failed,
          logItem: "Roof type not supported, please try with RevitExtrusionRoof or RevitFootprintRoof"
        );
        return appObj;
    }

    Doc.Regenerate();

    try
    {
      CreateVoids(revitRoof, speckleRoof);
    }
    catch (Exception ex) when (!ex.IsFatal())
    {
      SpeckleLog.Logger.LogDefaultError(ex);
      appObj.Update(logItem: $"Could not create openings: {ex.Message}");
    }

    if (speckleRevitRoof != null)
    {
      SetInstanceParameters(revitRoof, speckleRevitRoof);
    }
    else
    {
      TrySetParam(revitRoof, BuiltInParameter.ROOF_LEVEL_OFFSET_PARAM, -baseOffset);
    }

    appObj.Update(status: ApplicationObject.State.Created, createdId: revitRoof.UniqueId, convertedItem: revitRoof);

    Doc.Regenerate();

    return appObj;
  }

  private Roof RoofToSpeckle(DB.RoofBase revitRoof, out List<string> notes)
  {
    notes = new List<string>();
    List<ICurve> profiles = null;

    var speckleRoof = new RevitRoof();

    switch (revitRoof)
    {
      //assigning correct type for when going back in Revit
      case FootPrintRoof footPrintRoof:
      {
        var speckleFootprintRoof = new RevitFootprintRoof
        {
          level = ConvertAndCacheLevel(footPrintRoof, BuiltInParameter.ROOF_BASE_LEVEL_PARAM),
          cutOffLevel = ConvertAndCacheLevel(footPrintRoof, BuiltInParameter.ROOF_UPTO_LEVEL_PARAM),
          slope = GetParamValue<double?>(footPrintRoof, BuiltInParameter.ROOF_SLOPE) //NOTE: can be null if the sides have different slopes
        };

        var slopeArrow = GetSlopeArrow(footPrintRoof);
        if (slopeArrow != null)
        {
          var tail = GetSlopeArrowTail(slopeArrow, Doc);
          var head = GetSlopeArrowHead(slopeArrow, Doc);
          var tailOffset = GetSlopeArrowTailOffset(slopeArrow, Doc);
          var headOffset = GetSlopeArrowHeadOffset(slopeArrow, Doc, tailOffset, out _);

          var newTail = new Geometry.Point(tail.x, tail.y, tailOffset);
          var newHead = new Geometry.Point(head.x, head.y, headOffset);
          profiles = GetProfiles(revitRoof, newTail, newHead);
        }

        speckleRoof = speckleFootprintRoof;
        break;
      }
      case ExtrusionRoof revitExtrusionRoof:
      {
        var speckleExtrusionRoof = new RevitExtrusionRoof
        {
          start = GetParamValue<double>(revitExtrusionRoof, BuiltInParameter.EXTRUSION_START_PARAM),
          end = GetParamValue<double>(revitExtrusionRoof, BuiltInParameter.EXTRUSION_END_PARAM)
        };
        var plane = revitExtrusionRoof.GetProfile().get_Item(0).SketchPlane.GetPlane();
        speckleExtrusionRoof.referenceLine = new Line(
          PointToSpeckle(plane.Origin.Add(plane.XVec.Normalize().Negate()), revitRoof.Document),
          PointToSpeckle(plane.Origin, revitRoof.Document),
          ModelUnits
        ); //TODO: test!
        speckleExtrusionRoof.level = ConvertAndCacheLevel(
          revitExtrusionRoof,
          BuiltInParameter.ROOF_CONSTRAINT_LEVEL_PARAM
        );
        speckleRoof = speckleExtrusionRoof;
        break;
      }
    }
    var elementType = revitRoof.Document.GetElement(revitRoof.GetTypeId()) as ElementType;
    speckleRoof.type = elementType.Name;
    speckleRoof.family = elementType.FamilyName;

    if (profiles == null)
    {
      profiles = GetProfiles(revitRoof);
    }

    // TODO handle case if not one of our supported roofs
    if (profiles.Any())
    {
      speckleRoof.outline = profiles[0];
      if (profiles.Count > 1)
      {
        speckleRoof.voids = profiles.Skip(1).ToList();
      }
    }

    GetAllRevitParamsAndIds(
      speckleRoof,
      revitRoof,
      new List<string>
      {
        "ROOF_CONSTRAINT_LEVEL_PARAM",
        "ROOF_BASE_LEVEL_PARAM",
        "ROOF_UPTO_LEVEL_PARAM",
        "EXTRUSION_START_PARAM",
        "EXTRUSION_END_PARAM",
        "ROOF_SLOPE"
      }
    );

    speckleRoof.displayValue = GetElementDisplayValue(revitRoof);

    GetHostedElements(speckleRoof, revitRoof, out List<string> hostedNotes);
    if (hostedNotes.Any())
    {
      notes.AddRange(hostedNotes);
    }

    return speckleRoof;
  }

  //Nesting the various profiles into a polycurve segments
  private List<ICurve> GetProfiles(DB.RoofBase roof, Geometry.Point tailPoint = null, Geometry.Point headPoint = null)
  {
    // TODO handle case if not one of our supported roofs
    var profiles = new List<ICurve>();

    switch (roof)
    {
      case FootPrintRoof footprint:
      {
        ModelCurveArrArray crvLoops = footprint.GetProfiles();
        ModelCurve definesRoofSlope = null;
        double roofSlope = 0;

        // if headpoint and tailpoint are not null, it means that the user is using a
        // slope arrow to define the slope. Slope arrows are not creatable via the api
        // so we have to translate that into sloped segments.
        // if the user's slope arrow is not perpendicular to the segment that it is attached to
        // then it will not work (this is just a limitation of sloped segments in Revit)
        // see this api wish https://forums.autodesk.com/t5/revit-ideas/api-access-to-create-amp-modify-slope-arrow/idi-p/6700081
        if (tailPoint != null && headPoint != null)
        {
          for (var i = 0; i < crvLoops.Size; i++)
          {
            if (definesRoofSlope != null)
            {
              break;
            }

            var crvLoop = crvLoops.get_Item(i);
            var poly = new Polycurve(ModelUnits);
            foreach (DB.ModelCurve curve in crvLoop)
            {
              if (curve == null)
              {
                continue;
              }

              if (!(curve.Location is DB.LocationCurve c))
              {
                continue;
              }

              if (!(c.Curve is DB.Line line))
              {
                continue;
              }

              var start = PointToSpeckle(line.GetEndPoint(0), roof.Document);
              var end = PointToSpeckle(line.GetEndPoint(1), roof.Document);

              if (!IsBetween(start, end, tailPoint))
              {
                continue;
              }

              if (!CheckOrtho(start.x, start.y, end.x, end.y, tailPoint.x, tailPoint.y, headPoint.x, headPoint.y))
              {
                break;
              }

              definesRoofSlope = curve;
              var distance = Math.Sqrt(
                (Math.Pow(headPoint.x - tailPoint.x, 2) + Math.Pow(headPoint.y - tailPoint.y, 2))
              );
              roofSlope = Math.Atan((headPoint.z - tailPoint.z) / distance) * 180 / Math.PI;
              break;
            }
          }
        }

        for (var i = 0; i < crvLoops.Size; i++)
        {
          var crvLoop = crvLoops.get_Item(i);
          var poly = new Polycurve(ModelUnits);
          foreach (DB.ModelCurve curve in crvLoop)
          {
            if (curve == null)
            {
              continue;
            }

            var segment = CurveToSpeckle(curve.GeometryCurve, roof.Document) as Base; //it's a safe casting
            if (definesRoofSlope != null && curve == definesRoofSlope)
            {
              segment["slopeAngle"] = roofSlope;
              segment["isSloped"] = true;
              segment["offset"] = tailPoint.z;
            }
            else
            {
              segment["slopeAngle"] = GetParamValue<double>(curve, BuiltInParameter.ROOF_SLOPE);
              segment["isSloped"] = GetParamValue<bool>(curve, BuiltInParameter.ROOF_CURVE_IS_SLOPE_DEFINING);
              segment["offset"] = GetParamValue<double>(curve, BuiltInParameter.ROOF_CURVE_HEIGHT_OFFSET);
            }
            poly.segments.Add(segment as ICurve);

            //roud profiles are returned duplicated!
            if (curve is ModelArc arc && RevitVersionHelper.IsCurveClosed(arc.GeometryCurve))
            {
              break;
            }
          }
          profiles.Add(poly);
        }

        break;
      }
      case ExtrusionRoof extrusion:
      {
        var crvloop = extrusion.GetProfile();
        var poly = new Polycurve(ModelUnits);
        foreach (DB.ModelCurve curve in crvloop)
        {
          if (curve == null)
          {
            continue;
          }

          poly.segments.Add(CurveToSpeckle(curve.GeometryCurve, roof.Document));
        }
        profiles.Add(poly);
        break;
      }
    }
    return profiles;
  }

  // checks if point c is between a and b
  private bool IsBetween(Geometry.Point a, Geometry.Point b, Geometry.Point c)
  {
    var crossproduct = (c.y - a.y) * (b.x - a.x) - (c.x - a.x) * (b.y - a.y);

    // compare versus epsilon for floating point values, or != 0 if using integers
    if (Math.Abs(crossproduct) > TOLERANCE)
    {
      return false;
    }

    var dotProduct = (c.x - a.x) * (b.x - a.x) + (c.y - a.y) * (b.y - a.y);
    if (dotProduct < 0)
    {
      return false;
    }

    var squaredLengthBA = (b.x - a.x) * (b.x - a.x) + (b.y - a.y) * (b.y - a.y);
    if (dotProduct > squaredLengthBA)
    {
      return false;
    }

    return true;
  }

  private bool CheckOrtho(double x1, double y1, double x2, double y2, double x3, double y3, double x4, double y4)
  {
    double m1,
      m2;

    // Both lines have infinite slope
    if (Math.Abs(x2 - x1) < TOLERANCE && Math.Abs(x4 - x3) < TOLERANCE)
    {
      return false;
    }
    // Only line 1 has infinite slope
    else if (Math.Abs(x2 - x1) < TOLERANCE)
    {
      m2 = (y4 - y3) / (x4 - x3);
      if (Math.Abs(m2) < TOLERANCE)
      {
        return true;
      }
      else
      {
        return false;
      }
    }
    // Only line 2 has infinite slope
    else if (Math.Abs(x4 - x3) < TOLERANCE)
    {
      m1 = (y2 - y1) / (x2 - x1);
      if (Math.Abs(m1) < TOLERANCE)
      {
        return true;
      }
      else
      {
        return false;
      }
    }
    else
    {
      // Find slopes of the lines
      m1 = (y2 - y1) / (x2 - x1);
      m2 = (y4 - y3) / (x4 - x3);

      // Check if their product is -1
      if (Math.Abs(m1 * m2 + 1) < TOLERANCE)
      {
        return true;
      }
      else
      {
        return false;
      }
    }
  }
}
