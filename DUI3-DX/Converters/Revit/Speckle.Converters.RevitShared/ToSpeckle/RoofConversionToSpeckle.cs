using Speckle.Converters.Common.Objects;
using Speckle.Converters.Common;
using Objects;
using Speckle.Converters.RevitShared.Helpers;
using Speckle.Core.Models;
using Speckle.Core.Models.Extensions;
using RevitRoof = Objects.BuiltElements.Revit.RevitRoof.RevitRoof;
using Autodesk.Revit.DB;
using Objects.BuiltElements.Revit.RevitRoof;
using Objects.BuiltElements;
using Objects.Geometry;
using Objects.Structural.Analysis;

namespace Speckle.Converters.RevitShared.ToSpeckle;

[NameAndRankValue(nameof(DB.RoofBase), 0)]
public class RoofConversionToSpeckle : BaseConversionToSpeckle<DB.RoofBase, RevitRoof>
{
  private readonly IRawConversion<DB.Curve, ICurve> _curveConverter;
  private readonly IRawConversion<DB.Level, SOBR.RevitLevel> _levelConverter;
  private readonly IRawConversion<DB.ModelCurveArray, SOG.Polycurve> _modelCurveArrayConverter;
  private readonly IRawConversion<DB.ModelCurveArrArray, SOG.Polycurve[]> _modelCurveArrArrayConverter;
  private readonly IRawConversion<DB.XYZ, SOG.Point> _pointConverter;
  private readonly ParameterValueExtractor _parameterValueExtractor;
  private readonly RevitConversionContextStack _contextStack;
  private readonly DisplayValueExtractor _displayValueExtractor;
  private readonly HostedElementConversionToSpeckle _hostedElementConverter;
  private readonly ParameterObjectAssigner _parameterObjectAssigner;

  public RoofConversionToSpeckle(
    IRawConversion<DB.Curve, ICurve> curveConverter,
    IRawConversion<DB.Level, SOBR.RevitLevel> levelConverter,
    RevitConversionContextStack contextStack,
    ParameterValueExtractor parameterValueExtractor,
    DisplayValueExtractor displayValueExtractor,
    IRawConversion<DB.CurveArray, SOG.Polycurve> curveArrayConverter,
    HostedElementConversionToSpeckle hostedElementConverter,
    ParameterObjectAssigner parameterObjectAssigner
  )
  {
    _curveConverter = curveConverter;
    _levelConverter = levelConverter;
    _contextStack = contextStack;
    _parameterValueExtractor = parameterValueExtractor;
    _displayValueExtractor = displayValueExtractor;
    _modelCurveArrayConverter = curveArrayConverter;
    _hostedElementConverter = hostedElementConverter;
    _parameterObjectAssigner = parameterObjectAssigner;
  }

  public RevitRoof Convert(DB.RoofBase revitRoof, RevitRoof speckleRoof)
  {
    List<ICurve>? profiles = null;

    var elementType = revitRoof.Document.GetElement(revitRoof.GetTypeId()) as ElementType;
    speckleRoof.type = elementType.Name;
    speckleRoof.family = elementType.FamilyName;

    if (profiles == null)
    {
      profiles = GetProfiles(revitRoof);
    }

    if (profiles.Count > 0)
    {
      speckleRoof.outline = profiles[0];
      if (profiles.Count > 1)
      {
        speckleRoof.voids = profiles.Skip(1).ToList();
      }
    }

    _parameterObjectAssigner.AssignParametersToBase(revitRoof, speckleRoof);
    speckleRoof.displayValue = _displayValueExtractor.GetDisplayValue(revitRoof);

    GetHostedElements(speckleRoof, revitRoof, out List<string> hostedNotes);

    return speckleRoof;
  }

  public override RevitRoof RawConvert(DB.RoofBase revitRoof)
  {
    RevitRoof speckleRoof;
    List<ICurve>? profiles = null;

    if (revitRoof is DB.FootPrintRoof footPrintRoof)
    {
      speckleRoof = GetSpeckleFootPrintRoof(footPrintRoof);
    }
    else if (revitRoof is DB.ExtrusionRoof extrusionRoof)
    {
      speckleRoof = GetSpeckleExtrusionRoof(extrusionRoof);
    }
    else
    {
      throw new SpeckleConversionException($"Unsupported roof type: {revitRoof.GetType()}");
    }

    var elementType = revitRoof.Document.GetElement(revitRoof.GetTypeId()) as ElementType;
    speckleRoof.type = elementType.Name;
    speckleRoof.family = elementType.FamilyName;

    if (profiles == null)
    {
      profiles = GetProfiles(revitRoof);
    }

    if (profiles.Count > 0)
    {
      speckleRoof.outline = profiles[0];
      if (profiles.Count > 1)
      {
        speckleRoof.voids = profiles.Skip(1).ToList();
      }
    }

    _parameterObjectAssigner.AssignParametersToBase(revitRoof, speckleRoof);
    speckleRoof.displayValue = _displayValueExtractor.GetDisplayValue(revitRoof);

    GetHostedElements(speckleRoof, revitRoof, out List<string> hostedNotes);

    return speckleRoof;
  }

  private RevitExtrusionRoof GetSpeckleExtrusionRoof(ExtrusionRoof extrusionRoof)
  {
    var speckleExtrusionRoof = new RevitExtrusionRoof
    {
      start = _parameterValueExtractor.GetValueAsDouble(extrusionRoof, BuiltInParameter.EXTRUSION_START_PARAM),
      end = _parameterValueExtractor.GetValueAsDouble(extrusionRoof, BuiltInParameter.EXTRUSION_END_PARAM)
    };
    var plane = extrusionRoof.GetProfile().get_Item(0).SketchPlane.GetPlane();
    speckleExtrusionRoof.referenceLine = new SOG.Line(
      _pointConverter.RawConvert(plane.Origin.Add(plane.XVec.Normalize().Negate())),
      _pointConverter.RawConvert(plane.Origin)
    );
    var level = _parameterValueExtractor.GetValueAsDocumentObject<DB.Level>(
      extrusionRoof,
      DB.BuiltInParameter.ROOF_CONSTRAINT_LEVEL_PARAM
    );
    speckleExtrusionRoof.level = _levelConverter.RawConvert(level);
    speckleExtrusionRoof.outline = _modelCurveArrayConverter.RawConvert(extrusionRoof.GetProfile());
    //profiles = _modelCurveArrayConverter.RawConvert(extrusionRoof.GetProfile());
    return speckleExtrusionRoof;
  }

  private RevitFootprintRoof GetSpeckleFootPrintRoof(FootPrintRoof footPrintRoof)
  {
    var baseLevel = _parameterValueExtractor.GetValueAsDocumentObject<DB.Level>(
      footPrintRoof,
      DB.BuiltInParameter.ROOF_BASE_LEVEL_PARAM
    );
    var topLevel = _parameterValueExtractor.GetValueAsDocumentObject<DB.Level>(
      footPrintRoof,
      DB.BuiltInParameter.ROOF_UPTO_LEVEL_PARAM
    );

    //NOTE: can be null if the sides have different slopes
    double? slope = _parameterValueExtractor.GetValueAsDoubleOrNull(footPrintRoof, DB.BuiltInParameter.ROOF_SLOPE);

    var speckleFootprintRoof = new RevitFootprintRoof
    {
      level = _levelConverter.RawConvert(baseLevel),
      cutOffLevel = _levelConverter.RawConvert(topLevel),
      slope = slope
    };

    var profiles = _modelCurveArrArrayConverter.RawConvert(footPrintRoof.GetProfiles());
    speckleFootprintRoof.outline = profiles.FirstOrDefault();
    speckleFootprintRoof.voids = profiles.Skip(1).ToList<ICurve>();

    return speckleFootprintRoof;
  }

  //Nesting the various profiles into a polycurve segments
  private List<ICurve> GetProfiles(DB.RoofBase roof, SOG.Point tailPoint = null, SOG.Point headPoint = null)
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

              var start = _pointConverter.RawConvert(line.GetEndPoint(0), roof.Document);
              var end = _pointConverter.RawConvert(line.GetEndPoint(1), roof.Document);

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
              segment["slopeAngle"] = _parameterValueExtractor.GetValueAsDoubleOrNull(
                curve,
                BuiltInParameter.ROOF_SLOPE
              );
              segment["isSloped"] = GetParamValue<bool>(curve, BuiltInParameter.ROOF_CURVE_IS_SLOPE_DEFINING);
              segment["offset"] = _parameterValueExtractor.GetValueAsDoubleOrNull(
                curve,
                BuiltInParameter.ROOF_CURVE_HEIGHT_OFFSET
              );
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
