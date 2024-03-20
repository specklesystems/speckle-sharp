using Objects;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Speckle.Converters.Rhino7.Geometry;

[NameAndRankValue(nameof(RG.PolyCurve), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class PolyCurveToSpeckleConverter : IHostObjectToSpeckleConversion, IRawConversion<RG.PolyCurve, SOG.Polycurve>
{
  public IRawConversion<RG.Curve, ICurve> CurveConverter { get; set; } // This created a circular dependency on the constructor, making it a property allows for the container to resolve it correctly
  private readonly IRawConversion<RG.Interval, SOP.Interval> _intervalConverter;
  private readonly IRawConversion<RG.Box, SOG.Box> _boxConverter;

  public PolyCurveToSpeckleConverter(
    IRawConversion<RG.Interval, SOP.Interval> intervalConverter,
    IRawConversion<RG.Box, SOG.Box> boxConverter
  )
  {
    _intervalConverter = intervalConverter;
    _boxConverter = boxConverter;
  }

  public Base Convert(object target) => RawConvert((RG.PolyCurve)target);

  public SOG.Polycurve RawConvert(RG.PolyCurve target)
  {
    var segments = new List<RG.Curve>();
    CurveSegments(segments, target, true);

    var myPoly = new SOG.Polycurve
    {
      closed = target.IsClosed,
      domain = _intervalConverter.RawConvert(target.Domain),
      length = target.GetLength(),
      bbox = _boxConverter.RawConvert(new RG.Box(target.GetBoundingBox(true))),
      segments = segments.Select(CurveConverter.RawConvert).ToList(),
      units = Units.Meters //TODO: Get Units from context
    };
    return myPoly;
  }

  // Proper explosion of polycurves:
  // (C) The Rutten David https://www.grasshopper3d.com/forum/topics/explode-closed-planar-curve-using-rhinocommon
  private bool CurveSegments(List<RG.Curve> L, RG.Curve crv, bool recursive)
  {
    if (crv == null)
    {
      return false;
    }

    RG.PolyCurve polycurve = crv as RG.PolyCurve;

    if (polycurve != null)
    {
      if (recursive)
      {
        polycurve.RemoveNesting();
      }

      RG.Curve[] segments = polycurve.Explode();

      if (segments == null)
      {
        return false;
      }

      if (segments.Length == 0)
      {
        return false;
      }

      if (recursive)
      {
        foreach (RG.Curve S in segments)
        {
          CurveSegments(L, S, recursive);
        }
      }
      else
      {
        foreach (RG.Curve S in segments)
        {
          L.Add(S.DuplicateShallow() as RG.Curve);
        }
      }

      return true;
    }

    //Nothing else worked, lets assume it's a nurbs curve and go from there...
    var nurbs = crv.ToNurbsCurve();
    if (nurbs == null)
    {
      return false;
    }

    double t0 = nurbs.Domain.Min;
    double t1 = nurbs.Domain.Max;
    double t;

    int LN = L.Count;

    do
    {
      if (!nurbs.GetNextDiscontinuity(RG.Continuity.C1_locus_continuous, t0, t1, out t))
      {
        break;
      }

      var trim = new RG.Interval(t0, t);
      if (trim.Length < 1e-10)
      {
        t0 = t;
        continue;
      }

      var M = nurbs.DuplicateCurve();
      M = M.Trim(trim);
      if (M.IsValid)
      {
        L.Add(M);
      }

      t0 = t;
    } while (true);

    if (L.Count == LN)
    {
      L.Add(nurbs);
    }

    return true;
  }
}
