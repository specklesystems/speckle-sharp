using Objects;
using Rhino;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Speckle.Converters.Rhino7.Geometry;

[NameAndRankValue(nameof(RG.PolyCurve), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class PolyCurveToSpeckleConverter : IHostObjectToSpeckleConversion, IRawConversion<RG.PolyCurve, SOG.Polycurve>
{
  public IRawConversion<RG.Curve, ICurve>? CurveConverter { get; set; } // This created a circular dependency on the constructor, making it a property allows for the container to resolve it correctly
  private readonly IRawConversion<RG.Interval, SOP.Interval> _intervalConverter;
  private readonly IRawConversion<RG.Box, SOG.Box> _boxConverter;
  private readonly IConversionContextStack<RhinoDoc, UnitSystem> _contextStack;

  public PolyCurveToSpeckleConverter(
    IRawConversion<RG.Interval, SOP.Interval> intervalConverter,
    IRawConversion<RG.Box, SOG.Box> boxConverter,
    IConversionContextStack<RhinoDoc, UnitSystem> contextStack
  )
  {
    _intervalConverter = intervalConverter;
    _boxConverter = boxConverter;
    _contextStack = contextStack;
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
      segments = segments.Select(CurveConverter!.RawConvert).ToList(),
      units = _contextStack.Current.SpeckleUnits
    };
    return myPoly;
  }

  // Proper explosion of polycurves:
  // (C) The Rutten David https://www.grasshopper3d.com/forum/topics/explode-closed-planar-curve-using-rhinocommon
  private bool CurveSegments(List<RG.Curve> curveList, RG.Curve crv, bool recursive)
  {
    if (crv == null)
    {
      return false;
    }

    if (crv is RG.PolyCurve polyCurve)
    {
      if (recursive)
      {
        polyCurve.RemoveNesting();
      }

      RG.Curve[] segments = polyCurve.Explode();

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
        foreach (RG.Curve s in segments)
        {
          CurveSegments(curveList, s, recursive);
        }
      }
      else
      {
        foreach (RG.Curve s in segments)
        {
          var dup = (RG.Curve)s.DuplicateShallow();
          curveList.Add(dup);
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

    int ln = curveList.Count;

    do
    {
      if (!nurbs.GetNextDiscontinuity(RG.Continuity.C1_locus_continuous, t0, t1, out double t))
      {
        break;
      }

      var trim = new RG.Interval(t0, t);
      if (trim.Length < 1e-10)
      {
        t0 = t;
        continue;
      }

      var m = nurbs.DuplicateCurve();
      m = m.Trim(trim);
      if (m.IsValid)
      {
        curveList.Add(m);
      }

      t0 = t;
    } while (true);

    if (curveList.Count == ln)
    {
      curveList.Add(nurbs);
    }

    return true;
  }
}
