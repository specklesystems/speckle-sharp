﻿using Rhino.Collections;
using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.Rhino7.ToHost.Raw;

public class PolylineToHostConverter
  : IRawConversion<SOG.Polyline, RG.Polyline>,
    IRawConversion<SOG.Polyline, RG.PolylineCurve>
{
  private readonly IRawConversion<IReadOnlyList<double>, Point3dList> _pointListConverter;
  private readonly IRawConversion<SOP.Interval, RG.Interval> _intervalConverter;

  public PolylineToHostConverter(
    IRawConversion<IReadOnlyList<double>, Point3dList> pointListConverter,
    IRawConversion<SOP.Interval, RG.Interval> intervalConverter
  )
  {
    _pointListConverter = pointListConverter;
    _intervalConverter = intervalConverter;
  }

  /// <summary>
  /// Converts a Speckle polyline object to a Rhino Polyline object.
  /// </summary>
  /// <param name="target">The Speckle polyline object to be converted.</param>
  /// <returns>The converted Rhino Polyline object.</returns>
  /// <remarks>⚠️ This conversion does NOT perform scaling.</remarks>
  /// <remarks>
  /// <br/>⚠️ This conversion does not preserve the curve domain.
  /// If you need it preserved you must request a conversion to
  /// <see cref="RG.PolylineCurve"/> conversion instead
  /// </remarks>
  public RG.Polyline RawConvert(SOG.Polyline target)
  {
    var points = _pointListConverter.RawConvert(target.value);

    if (target.closed)
    {
      points.Add(points[0]);
    }

    var poly = new RG.Polyline(points);

    return poly;
  }

  // POC: CNX-9271 Potential code-smell by directly implementing the interface. We should discuss this further but
  // since we're using the interfaces instead of the direct type, this may not be an issue.
  /// <summary>
  /// Converts a Speckle polyline object to a Rhino Polyline object.
  /// </summary>
  /// <param name="target">The Speckle polyline object to be converted.</param>
  /// <returns>The converted Rhino Polyline object.</returns>
  /// <remarks>⚠️ This conversion does NOT perform scaling.</remarks>
  RG.PolylineCurve IRawConversion<SOG.Polyline, RG.PolylineCurve>.RawConvert(SOG.Polyline target)
  {
    var poly = RawConvert(target).ToPolylineCurve();
    poly.Domain = _intervalConverter.RawConvert(target.domain);
    return poly;
  }
}
