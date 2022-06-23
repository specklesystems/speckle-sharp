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
    public List<ApplicationPlaceholderObject> RoofToNative(Roof speckleRoof)
    {
      if (speckleRoof.outline == null)
      {
        throw new Speckle.Core.Logging.SpeckleException("Only outline based Roof are currently supported.");
      }

      DB.RoofBase revitRoof = null;
      DB.Level level = null;
      var outline = CurveToNative(speckleRoof.outline);

      var speckleRevitRoof = speckleRoof as RevitRoof;
      if (speckleRevitRoof != null)
      {
        level = ConvertLevelToRevit(speckleRevitRoof.level);
      }
      else
      {
        level = ConvertLevelToRevit(LevelFromCurve(outline.get_Item(0)));
      }

      var roofType = GetElementType<RoofType>((Base)speckleRoof);

      var docObj = GetExistingElementByApplicationId(((Base)speckleRoof).applicationId);
      if (docObj != null && ReceiveMode == Speckle.Core.Kits.ReceiveMode.Ignore)
        return new List<ApplicationPlaceholderObject> { new ApplicationPlaceholderObject { applicationId = speckleRoof.applicationId, ApplicationGeneratedId = docObj.UniqueId, NativeObject = docObj } };
      if (docObj != null)
      {
        Doc.Delete(docObj.Id);
      }

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
                    revitFootprintRoof.set_SlopeAngle(curveArray.get_Item(i), (double)slopeAngle * Math.PI / 180);
                    hasSlopedSide = true;
                  }

                }

                if (offset != null)
                  revitFootprintRoof.set_Offset(curveArray.get_Item(i), (double)offset);
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
              var cutOffLevel = ConvertLevelToRevit(speckleFootprintRoof.cutOffLevel);
              TrySetParam(revitFootprintRoof, BuiltInParameter.ROOF_UPTO_LEVEL_PARAM, cutOffLevel);
            }

            revitRoof = revitFootprintRoof;
            break;
          }
        default:
          Report.LogConversionError(new Exception("Roof type not supported, please try with RevitExtrusionRoof or RevitFootprintRoof"));
          return null;

      }

      Doc.Regenerate();

      try
      {
        CreateVoids(revitRoof, speckleRoof);
      }
      catch (Exception ex)
      {
        Report.LogConversionError(new Exception($"Could not create openings in roof {speckleRoof.applicationId}", ex));
      }

      if (speckleRevitRoof != null)
      {
        SetInstanceParameters(revitRoof, speckleRevitRoof);
      }

      var placeholders = new List<ApplicationPlaceholderObject>() { new ApplicationPlaceholderObject { applicationId = speckleRevitRoof.applicationId, ApplicationGeneratedId = revitRoof.UniqueId, NativeObject = revitRoof } };

      var hostedElements = SetHostedElements(speckleRoof, revitRoof);
      placeholders.AddRange(hostedElements);
      Report.Log($"Created Roof {revitRoof.Id}");
      return placeholders;
    }

    private Roof RoofToSpeckle(DB.RoofBase revitRoof)
    {
      var profiles = GetProfiles(revitRoof);

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

      // TODO handle case if not one of our supported roofs
      if (profiles.Any())
      {
        speckleRoof.outline = profiles[0];
        if (profiles.Count > 1)
        {
          speckleRoof.voids = profiles.Skip(1).ToList();
        }
      }

      GetAllRevitParamsAndIds(speckleRoof, revitRoof,
        new List<string> { "ROOF_CONSTRAINT_LEVEL_PARAM", "ROOF_BASE_LEVEL_PARAM", "ROOF_UPTO_LEVEL_PARAM", "EXTRUSION_START_PARAM", "EXTRUSION_END_PARAM" });

      speckleRoof.displayValue = GetElementDisplayMesh(revitRoof, new Options() { DetailLevel = ViewDetailLevel.Fine, ComputeReferences = false });

      GetHostedElements(speckleRoof, revitRoof);
      Report.Log($"Converted Roof {revitRoof.Id}");
      return speckleRoof;
    }

    //Nesting the various profiles into a polycurve segments
    private List<ICurve> GetProfiles(DB.RoofBase roof)
    {
      // TODO handle case if not one of our supported roofs
      var profiles = new List<ICurve>();

      switch (roof)
      {
        case FootPrintRoof footprint:
          {
            ModelCurveArrArray crvLoops = footprint.GetProfiles();

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

                var segment = CurveToSpeckle(curve.GeometryCurve) as Base; //it's a safe casting
                segment["slopeAngle"] = GetParamValue<double>(curve, BuiltInParameter.ROOF_SLOPE);
                segment["isSloped"] = GetParamValue<bool>(curve, BuiltInParameter.ROOF_CURVE_IS_SLOPE_DEFINING);
                segment["offset"] = GetParamValue<double>(curve, BuiltInParameter.ROOF_CURVE_HEIGHT_OFFSET);
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

              poly.segments.Add(CurveToSpeckle(curve.GeometryCurve));
            }
            profiles.Add(poly);
            break;
          }
      }
      return profiles;
    }
  }
}
