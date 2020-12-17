using Autodesk.Revit.DB;
using Objects.BuiltElements;
using Objects.BuiltElements.Revit;
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
        throw new Exception("Only outline based Floor are currently supported.");
      }

      DB.RoofBase revitRoof = null;
      DB.Level level = null;
      var outline = CurveToNative(speckleRoof.outline);

      var speckleRevitRoof = speckleRoof as RevitRoof;
      if (speckleRevitRoof != null)
      {
        level = LevelToNative(speckleRevitRoof.level);
      }
      else
      {
        level = LevelToNative(LevelFromCurve(outline.get_Item(0)));
      }

      var roofType = GetElementType<RoofType>((Base)speckleRoof);

      var docObj = GetExistingElementByApplicationId(((Base)speckleRoof).applicationId);
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
            revitRoof = Doc.Create.NewExtrusionRoof(outline, plane, level, roofType, speckleExtrusionRoof.start, speckleExtrusionRoof.end);
            break;
          }

        case RevitFootprintRoof speckleFootprintRoof:
          {
            ModelCurveArray curveArray = new ModelCurveArray();
            var revitFootprintRoof = Doc.Create.NewFootPrintRoof(outline, level, roofType, out curveArray);
            var poly = speckleFootprintRoof.outline as Polycurve;
            if (poly != null)
            {
              for (var i = 0; i < curveArray.Size; i++)
              {
                var isSloped = ((Base)poly.segments[i])["isSloped"] as bool?;
                revitFootprintRoof.set_DefinesSlope(curveArray.get_Item(i), isSloped == true);

                try
                {
                  var slopeAngle = ((Base)poly.segments[i])["slopeAngle"] as double?;
                  revitFootprintRoof.set_SlopeAngle(curveArray.get_Item(i), (double)slopeAngle);
                }
                catch { }
                var offset = ((Base)poly.segments[i])["offset"] as double?;
                revitFootprintRoof.set_Offset(curveArray.get_Item(i), (double)offset);
              }
            }

            if (speckleFootprintRoof.cutOffLevel != null)
            {
              var cutOffLevel = LevelToNative(speckleFootprintRoof.cutOffLevel);
              TrySetParam(revitFootprintRoof, BuiltInParameter.ROOF_UPTO_LEVEL_PARAM, cutOffLevel);
            }

            revitRoof = revitFootprintRoof;
            break;
          }
        default:
          ConversionErrors.Add(new Error("Cannot create Roof", "Roof type not supported"));
          throw new Exception("Roof type not supported");

      }

      Doc.Regenerate();

      try
      {
        MakeOpeningsInRoof(revitRoof, speckleRoof.voids.ToList());
      }
      catch (Exception ex)
      {
        ConversionErrors.Add(new Error("Could not create holes in roof", ex.Message));
      }

      if (speckleRevitRoof != null)
      {
        SetInstanceParameters(revitRoof, speckleRevitRoof);
      }

      var placeholders = new List<ApplicationPlaceholderObject>() { new ApplicationPlaceholderObject { applicationId = speckleRevitRoof.applicationId, ApplicationGeneratedId = revitRoof.UniqueId, NativeObject = revitRoof } };


      var hostedElements = SetHostedElements(speckleRoof, revitRoof);
      placeholders.AddRange(hostedElements);

      return placeholders;
    }

    private void MakeOpeningsInRoof(DB.RoofBase roof, List<ICurve> holes)
    {
      foreach (var hole in holes)
      {
        var curveArray = CurveToNative(hole);
        Doc.Create.NewOpening(roof, curveArray, true);
      }
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
              cutOffLevel = ConvertAndCacheLevel(footPrintRoof, BuiltInParameter.ROOF_UPTO_LEVEL_PARAM)
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
            speckleExtrusionRoof.level = ConvertAndCacheLevel(revitExtrusionRoof, BuiltInParameter.ROOF_BASE_LEVEL_PARAM);
            speckleRoof = speckleExtrusionRoof;
            break;
          }
      }

      speckleRoof.type = Doc.GetElement(revitRoof.GetTypeId()).Name;

      // TODO handle case if not one of our supported roofs
      if (profiles.Any())
      {
        speckleRoof.outline = profiles[0];
        if (profiles.Count > 1)
        {
          speckleRoof.voids = profiles.Skip(1).ToList();
        }
      }

      GetRevitParameters(speckleRoof, revitRoof, new List<string> { "ROOF_BASE_LEVEL_PARAM", "ROOF_UPTO_LEVEL_PARAM", "EXTRUSION_START_PARAM", "EXTRUSION_END_PARAM" });

      var displayMesh = new Geometry.Mesh();
      (displayMesh.faces, displayMesh.vertices) = GetFaceVertexArrayFromElement(revitRoof, new Options() { DetailLevel = ViewDetailLevel.Fine, ComputeReferences = false });
      speckleRoof["@displayMesh"] = displayMesh;

      GetHostedElements(speckleRoof, revitRoof);

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

                var segment = CurveToSpeckle(curve.GeometryCurve) as Base; //it's a safe casting, should be improved tho...
                segment["slopeAngle"] = ParameterToSpeckle(curve.get_Parameter(BuiltInParameter.ROOF_SLOPE));
                segment["isSloped"] = ParameterToSpeckle(curve.get_Parameter(BuiltInParameter.ROOF_CURVE_IS_SLOPE_DEFINING));
                segment["offset"] = ParameterToSpeckle(curve.get_Parameter(BuiltInParameter.ROOF_CURVE_HEIGHT_OFFSET));
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