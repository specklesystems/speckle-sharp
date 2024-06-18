using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Rhino7.Interfaces;

namespace Speckle.Converters.Rhino7.ToSpeckle.Raw;

public class PointToSpeckleConverter : ITypedConverter<IRhinoPoint3d, SOG.Point>, ITypedConverter<IRhinoPoint, SOG.Point>
{
  private readonly IConversionContextStack<IRhinoDoc, RhinoUnitSystem> _contextStack;

  public PointToSpeckleConverter(IConversionContextStack<IRhinoDoc, RhinoUnitSystem> contextStack)
  {
    _contextStack = contextStack;
  }

  /// <summary>
  /// Converts a Rhino 3D point to a Speckle point.
  /// </summary>
  /// <param name="target">The Rhino 3D point to convert.</param>
  /// <returns>The converted Speckle point.</returns>
  public SOG.Point Convert(IRhinoPoint3d target) => new(target.X, target.Y, target.Z, _contextStack.Current.SpeckleUnits);

  public SOG.Point Convert(IRhinoPoint target) => Convert(target.Location);
}
