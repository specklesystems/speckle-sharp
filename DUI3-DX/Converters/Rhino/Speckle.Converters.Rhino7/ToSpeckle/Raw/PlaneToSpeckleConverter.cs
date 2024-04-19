using Rhino;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.Rhino7.ToSpeckle.Raw;

public class PlaneToSpeckleConverter : IRawConversion<RG.Plane, SOG.Plane>
{
  private readonly IRawConversion<RG.Vector3d, SOG.Vector> _vectorConverter;
  private readonly IRawConversion<RG.Point3d, SOG.Point> _pointConverter;
  private readonly IConversionContextStack<RhinoDoc, UnitSystem> _contextStack;

  public PlaneToSpeckleConverter(
    IRawConversion<RG.Vector3d, SOG.Vector> vectorConverter,
    IRawConversion<RG.Point3d, SOG.Point> pointConverter,
    IConversionContextStack<RhinoDoc, UnitSystem> contextStack
  )
  {
    _vectorConverter = vectorConverter;
    _pointConverter = pointConverter;
    _contextStack = contextStack;
  }

  /// <summary>
  /// Converts an instance of Rhino Plane to Speckle Plane.
  /// </summary>
  /// <param name="target">The instance of Rhino Plane to convert.</param>
  /// <returns>The converted instance of Speckle Plane.</returns>
  public SOG.Plane RawConvert(RG.Plane target) =>
    new(
      _pointConverter.RawConvert(target.Origin),
      _vectorConverter.RawConvert(target.ZAxis),
      _vectorConverter.RawConvert(target.XAxis),
      _vectorConverter.RawConvert(target.YAxis),
      _contextStack.Current.SpeckleUnits
    );
}
