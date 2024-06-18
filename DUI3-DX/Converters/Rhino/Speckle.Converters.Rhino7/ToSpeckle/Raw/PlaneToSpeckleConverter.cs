using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Rhino7.Interfaces;

namespace Speckle.Converters.Rhino7.ToSpeckle.Raw;

public class PlaneToSpeckleConverter : ITypedConverter<IRhinoPlane, SOG.Plane>
{
  private readonly ITypedConverter<IRhinoVector3d, SOG.Vector> _vectorConverter;
  private readonly ITypedConverter<IRhinoPoint3d, SOG.Point> _pointConverter;
  private readonly IConversionContextStack<IRhinoDoc, RhinoUnitSystem> _contextStack;

  public PlaneToSpeckleConverter(
    ITypedConverter<IRhinoVector3d, SOG.Vector> vectorConverter,
    ITypedConverter<IRhinoPoint3d, SOG.Point> pointConverter,
    IConversionContextStack<IRhinoDoc, RhinoUnitSystem> contextStack
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
  public SOG.Plane Convert(IRhinoPlane target) =>
    new(
      _pointConverter.Convert(target.Origin),
      _vectorConverter.Convert(target.ZAxis),
      _vectorConverter.Convert(target.XAxis),
      _vectorConverter.Convert(target.YAxis),
      _contextStack.Current.SpeckleUnits
    );
}
