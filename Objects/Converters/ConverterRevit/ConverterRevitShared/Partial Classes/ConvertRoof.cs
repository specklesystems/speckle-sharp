using Autodesk.Revit.DB;
using Objects.BuiltElements;
using Objects.BuiltElements.Revit.RevitRoof;
using Objects.Geometry;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using DB = Autodesk.Revit.DB;
using Line = Objects.Geometry.Line;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
    public ApplicationObject RoofToNative(Roof speckleRoof)
    {
      var docObj = GetExistingElementByApplicationId((speckleRoof).applicationId);
      var appObj = new ApplicationObject(speckleRoof.id, speckleRoof.speckle_type) { applicationId = speckleRoof.applicationId };
      
      // skip if element already exists in doc & receive mode is set to ignore
      if (IsIgnore(docObj, appObj, out appObj))
        return appObj;

      if (speckleRoof.outline == null)
      {
        appObj.Update(status: ApplicationObject.State.Failed, logItem: "Roof outline was null");
        return appObj;
      }

      DB.RoofBase revitRoof = null;
      DB.Level level = null;
      var outline = CurveToNative(speckleRoof.outline);

      var speckleRevitRoof = speckleRoof as RevitRoof;
      var levelState = ApplicationObject.State.Unknown;
      if (speckleRevitRoof != null)
        level = ConvertLevelToRevit(speckleRevitRoof.level, out levelState);
      else
        level = ConvertLevelToRevit(LevelFromCurve(outline.get_Item(0)), out levelState);

      if (!GetElementType<RoofType>(speckleRoof, appObj, out RoofType roofType))
      {
        appObj.Update(status: ApplicationObject.State.Failed);
        return appObj;
      }

      if (docObj != null)
        Doc.Delete(docObj.Id);

      switch (speckleRoof)
      {
        case RevitExtrusionRoof speckleExtrusionRoof:
          {
            var referenceLine = LineToNative(speckleExtrusionRoof.referenceLine);
            var norm = GetPerpendicular(referenceLine.GetEndPoint(0) - referenceLine.GetEndPoint(1)).Negate();
            ReferencePlane plane = Doc.Create.NewReferencePlane(referenceLine.GetEndPoint(0),
              referenceLine.GetEndPoint(1),
              norm,
              Doc.ActiveView);
            //create floor without a type
            var start = ScaleToNative(speckleExtrusionRoof.start, speckleExtrusionRoof.units);
            var end = ScaleToNative(speckleExtrusionRoof.end, speckleExtrusionRoof.units);
            revitRoof = Doc.Create.NewExtrusionRoof(outline, plane, level, roofType, start, end);
            break;
          }
        case RevitFootprintRoof speckleFootprintRoof:
          {
            ModelCurveArray curveArray = new ModelCurveArray();
            var revitFootprintRoof = Doc.Create.NewFootPrintRoof(outline, level, roofType, out curveArray);

            // if the roof is a curtain roof then set the mullions at the borders
            if (revitFootprintRoof.CurtainGrids != null && speckleFootprintRoof["elements"] is List<Base> elements && elements.Count != 0)
            {
              // TODO: Create a new type instead of overriding the type. This could affect other elements
              var param = roofType.get_Parameter(BuiltInParameter.AUTO_MULLION_BORDER1_GRID1);
              var type = Doc.GetElement(param.AsElementId());
              if (type == null)
              {
                // assuming first mullion is the desired mullion for the whole roof...
                GetElementType<MullionType>(elements.Where(b=>b is BuiltElements.Revit.FamilyInstance f).First(), new ApplicationObject("", ""), out MullionType mullionType);
                TrySetParam(roofType, BuiltInParameter.AUTO_MULLION_BORDER1_GRID1, mullionType);
                TrySetParam(roofType, BuiltInParameter.AUTO_MULLION_BORDER1_GRID2, mullionType);
                TrySetParam(roofType, BuiltInParameter.AUTO_MULLION_BORDER2_GRID1, mullionType);
                TrySetParam(roofType, BuiltInParameter.AUTO_MULLION_BORDER2_GRID2, mullionType);
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
                  revitFootprintRoof.set_Offset(curveArray.get_Item(i), ScaleToNative((double)offset, speckleFootprintRoof.units));
              }
            }

            //this is for schema builder specifically
            //if no roof edge has a slope defined but a slope angle is defined on the roof
            //set each edge to have that slope
            if (!hasSlopedSide && speckleFootprintRoof.slope != null && speckleFootprintRoof.slope != 0)
            {
              for (var i = 0; i < curveArray.Size; i++)
                revitFootprintRoof.set_DefinesSlope(curveArray.get_Item(i), true);

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
          appObj.Update(status: ApplicationObject.State.Failed, logItem: "Roof type not supported, please try with RevitExtrusionRoof or RevitFootprintRoof");
          return appObj;
      }

      Doc.Regenerate();

      try
      {
        CreateVoids(revitRoof, speckleRoof);
      }
      catch (Exception ex)
      {
        appObj.Update(logItem: $"Could not create openings: {ex.Message}");
      }

      if (speckleRevitRoof != null)
        SetInstanceParameters(revitRoof, speckleRevitRoof);

      appObj.Update(status: ApplicationObject.State.Created, createdId: revitRoof.UniqueId, convertedItem: revitRoof);

      Doc.Regenerate();
      appObj = SetHostedElements(speckleRoof, revitRoof, appObj);
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

            // MEGA HACK to get the slope arrow of a roof which is technically not accessable by the api
            // https://forums.autodesk.com/t5/revit-api-forum/access-parameters-of-slope-arrow/td-p/8134470
            List<ElementId> deleted = null;
            Geometry.Point tail = null;
            Geometry.Point head = null;
            double tailOffset = 0;
            double headOffset = 0;
            using (Transaction t = new Transaction(Doc, "TTT"))
            {
              t.Start();
              deleted = Doc.Delete(footPrintRoof.Id).ToList();
              t.RollBack();
            }
            foreach (ElementId id in deleted)
            {
              ModelLine l = Doc.GetElement(id) as ModelLine;
              if (l == null) continue;
              if (!l.Name.Equals("Slope Arrow")) continue;

              tail = PointToSpeckle(((LocationCurve)l.Location).Curve.GetEndPoint(0));
              head = PointToSpeckle(((LocationCurve)l.Location).Curve.GetEndPoint(1));
              tailOffset = GetParamValue<double>(l, BuiltInParameter.SLOPE_START_HEIGHT);
              headOffset = GetParamValue<double>(l, BuiltInParameter.SLOPE_END_HEIGHT);

              break;
            }

            // these two values are not null then the slope arrow exists and we need to capture that
            if (tail != null && head != null)
            {
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
            speckleExtrusionRoof.referenceLine =
            new Line(PointToSpeckle(plane.Origin.Add(plane.XVec.Normalize().Negate())), PointToSpeckle(plane.Origin), ModelUnits); //TODO: test!
            speckleExtrusionRoof.level = ConvertAndCacheLevel(revitExtrusionRoof, BuiltInParameter.ROOF_CONSTRAINT_LEVEL_PARAM);
            speckleRoof = speckleExtrusionRoof;
            break;
          }
      }
      var elementType = revitRoof.Document.GetElement(revitRoof.GetTypeId()) as ElementType;
      speckleRoof.type = elementType.Name;
      speckleRoof.family = elementType.FamilyName;

      if (profiles == null)
        profiles = GetProfiles(revitRoof);

      // TODO handle case if not one of our supported roofs
      if (profiles.Any())
      {
        speckleRoof.outline = profiles[0];
        if (profiles.Count > 1)
          speckleRoof.voids = profiles.Skip(1).ToList();
      }

      GetAllRevitParamsAndIds(speckleRoof, revitRoof,
        new List<string> { "ROOF_CONSTRAINT_LEVEL_PARAM", "ROOF_BASE_LEVEL_PARAM", "ROOF_UPTO_LEVEL_PARAM", "EXTRUSION_START_PARAM", "EXTRUSION_END_PARAM" });

      speckleRoof.displayValue = GetElementDisplayMesh(revitRoof, new Options() { DetailLevel = ViewDetailLevel.Fine, ComputeReferences = false });

      GetHostedElements(speckleRoof, revitRoof, out List<string> hostedNotes);
      if (hostedNotes.Any()) notes.AddRange(hostedNotes);
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
                  break;

                var crvLoop = crvLoops.get_Item(i);
                var poly = new Polycurve(ModelUnits);
                foreach (DB.ModelCurve curve in crvLoop)
                {
                  if (curve == null)
                    continue;
                  if (!(curve.Location is DB.LocationCurve c))
                    continue;
                  if (!(c.Curve is DB.Line line))
                    continue;

                  var start = PointToSpeckle(line.GetEndPoint(0));
                  var end = PointToSpeckle(line.GetEndPoint(1));

                  if (!IsBetween(start, end, tailPoint))
                    continue;

                  if (!CheckOrtho(start.x, start.y, end.x, end.y, tailPoint.x, tailPoint.y, headPoint.x, headPoint.y))
                    break;

                  definesRoofSlope = curve;
                  var distance = Math.Sqrt((Math.Pow(headPoint.x - tailPoint.x, 2) + Math.Pow(headPoint.y - tailPoint.y, 2)));
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
                  continue;

                var segment = CurveToSpeckle(curve.GeometryCurve) as Base; //it's a safe casting
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
                  break;
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
                continue;

              poly.segments.Add(CurveToSpeckle(curve.GeometryCurve));
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
          return false;

      var dotProduct = (c.x - a.x) * (b.x - a.x) + (c.y - a.y) * (b.y - a.y);
      if (dotProduct < 0)
        return false;

      var squaredLengthBA = (b.x - a.x) * (b.x - a.x) + (b.y - a.y) * (b.y - a.y);
      if (dotProduct > squaredLengthBA)
        return false;

      return true;
    }

    private bool CheckOrtho(double x1, double y1, double x2, double y2, double x3, double y3, double x4, double y4)
    {
      double m1, m2;

      // Both lines have infinite slope
      if (Math.Abs(x2 - x1) < TOLERANCE && Math.Abs(x4 - x3) < TOLERANCE)
        return false;

      // Only line 1 has infinite slope
      else if (Math.Abs(x2 - x1) < TOLERANCE)
      {
        m2 = (y4 - y3) / (x4 - x3);
        if (Math.Abs(m2) < TOLERANCE)
          return true;
        else
          return false;
      }

      // Only line 2 has infinite slope
      else if (Math.Abs(x4 - x3) < TOLERANCE)
      {
        m1 = (y2 - y1) / (x2 - x1);
        if (Math.Abs(m1) < TOLERANCE)
          return true;
        else
          return false;
      }

      else
      {
        // Find slopes of the lines
        m1 = (y2 - y1) / (x2 - x1);
        m2 = (y4 - y3) / (x4 - x3);

        // Check if their product is -1
        if (Math.Abs(m1 * m2 + 1) < TOLERANCE)
          return true;
        else
          return false;
      }
    }
  }
}
