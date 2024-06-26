using Speckle.Converters.Common.Objects;
using Speckle.Rhino7.Interfaces;

namespace Speckle.Converters.Rhino7.ToHost.Raw;

public class IntervalToHostConverter : ITypedConverter<SOP.Interval, IRhinoInterval>
{
  private readonly IRhinoIntervalFactory _rhinoIntervalFactory;

  public IntervalToHostConverter(IRhinoIntervalFactory rhinoIntervalFactory)
  {
    _rhinoIntervalFactory = rhinoIntervalFactory;
  }

  /// <summary>
  /// Converts a Speckle Interval object to a Rhino.Geometry.Interval object.
  /// </summary>
  /// <param name="target">The Speckle Interval to convert.</param>
  /// <returns>The converted Rhino.Geometry.Interval object.</returns>
  /// <exception cref="ArgumentException">Thrown when the start or end value of the Interval is null.</exception>
  /// <remarks>⚠️ This conversion does NOT perform scaling.</remarks>
  public IRhinoInterval Convert(SOP.Interval target)
  {
    if (!target.start.HasValue || !target.end.HasValue) // POC: CNX-9272 Interval start and end being nullable makes no sense.
    {
      throw new ArgumentException("Interval start/end cannot be null");
    }

    return _rhinoIntervalFactory.Create(target.start.Value, target.end.Value);
  }
}
