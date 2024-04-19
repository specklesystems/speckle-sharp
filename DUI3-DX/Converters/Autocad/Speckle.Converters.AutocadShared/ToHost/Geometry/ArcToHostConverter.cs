using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.Autocad.ToHost.Geometry;

[NameAndRankValue(nameof(SOG.Arc), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class ArcToHostConverter : ISpeckleObjectToHostConversion, IRawConversion<SOG.Arc, ADB.Arc>
{
  private readonly IRawConversion<SOG.Arc, AG.CircularArc3d> _arcConverter;
  private readonly IRawConversion<SOG.Plane, AG.Plane> _planeConverter;
  private readonly IConversionContextStack<Document, ADB.UnitsValue> _contextStack;

  public ArcToHostConverter(
    IRawConversion<SOG.Arc, AG.CircularArc3d> arcConverter,
    IRawConversion<SOG.Plane, AG.Plane> planeConverter,
    IConversionContextStack<Document, ADB.UnitsValue> contextStack
  )
  {
    _arcConverter = arcConverter;
    _planeConverter = planeConverter;
    _contextStack = contextStack;
  }

  public object Convert(Base target) => RawConvert((SOG.Arc)target);

  public ADB.Arc RawConvert(SOG.Arc target)
  {
    // the most reliable method to convert to autocad convention is to calculate from start, end, and midpoint
    // because of different plane & start/end angle conventions
    AG.CircularArc3d circularArc = _arcConverter.RawConvert(target);

    // calculate adjusted start and end angles from circularArc reference
    AG.Plane plane = _planeConverter.RawConvert(target.plane);
    double angle = circularArc.ReferenceVector.AngleOnPlane(plane);
    double startAngle = circularArc.StartAngle + angle;
    double endAngle = circularArc.EndAngle + angle;

    return new(circularArc.Center, circularArc.Normal, circularArc.Radius, startAngle, endAngle);
  }
}
