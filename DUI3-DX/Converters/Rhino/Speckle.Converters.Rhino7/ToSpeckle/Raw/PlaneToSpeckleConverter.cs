using Rhino;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.Rhino7.ToSpeckle.Raw;

public class PlaneToSpeckleConverter : ITypedConverter<RG.Plane, SOG.Plane>
{
  private readonly ITypedConverter<RG.Vector3d, SOG.Vector> _vectorConverter;
  private readonly ITypedConverter<RG.Point3d, SOG.Point> _pointConverter;
  private readonly IConversionContextStack<RhinoDoc, UnitSystem> _contextStack;

  public PlaneToSpeckleConverter(
    ITypedConverter<RG.Vector3d, SOG.Vector> vectorConverter,
    ITypedConverter<RG.Point3d, SOG.Point> pointConverter,
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
  public SOG.Plane Convert(RG.Plane target) =>
    new(
      _pointConverter.Convert(target.Origin),
      _vectorConverter.Convert(target.ZAxis),
      _vectorConverter.Convert(target.XAxis),
      _vectorConverter.Convert(target.YAxis),
      _contextStack.Current.SpeckleUnits
    );
}
