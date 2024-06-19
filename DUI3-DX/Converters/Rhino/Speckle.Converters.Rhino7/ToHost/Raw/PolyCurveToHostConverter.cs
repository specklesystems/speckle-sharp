using Objects;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Kits;
using Speckle.Rhino7.Interfaces;

namespace Speckle.Converters.Rhino7.ToHost.Raw;

public class PolyCurveToHostConverter : ITypedConverter<SOG.Polycurve, IRhinoPolyCurve>
{
  private readonly ITypedConverter<ICurve, IRhinoCurve> _curveConverter;
  private readonly ITypedConverter<SOP.Interval, IRhinoInterval> _intervalConverter;
  private readonly IRhinoCurveFactory _rhinoCurveFactory;

  public PolyCurveToHostConverter(
    ITypedConverter<SOP.Interval, IRhinoInterval> intervalConverter,
    ITypedConverter<ICurve, IRhinoCurve> curveConverter,
    IRhinoCurveFactory rhinoCurveFactory
  )
  {
    _intervalConverter = intervalConverter;
    _curveConverter = curveConverter;
    _rhinoCurveFactory = rhinoCurveFactory;
  }

  /// <summary>
  /// Converts a SpecklePolyCurve object to a Rhino PolyCurve object.
  /// </summary>
  /// <param name="target">The SpecklePolyCurve object to convert.</param>
  /// <returns>The converted Rhino PolyCurve object.</returns>
  /// <remarks>⚠️ This conversion does NOT perform scaling.</remarks>
  public IRhinoPolyCurve Convert(SOG.Polycurve target)
  {
    IRhinoPolyCurve result = _rhinoCurveFactory.Create();

    foreach (var segment in target.segments)
    {
      var childCurve = _curveConverter.Convert(segment);
      bool success = result.AppendSegment(childCurve);
      if (!success)
      {
        throw new ConversionException($"Failed to append segment {segment}");
      }
    }

    result.Domain = _intervalConverter.Convert(target.domain);

    return result;
  }
}
