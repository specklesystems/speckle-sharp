using Rhino;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.Rhino7.ToSpeckle.Raw;

public class PointToSpeckleConverter : IRawConversion<RG.Point3d, SOG.Point>, IRawConversion<RG.Point, SOG.Point>
{
  private readonly IConversionContextStack<RhinoDoc, UnitSystem> _contextStack;

  public PointToSpeckleConverter(IConversionContextStack<RhinoDoc, UnitSystem> contextStack)
  {
    _contextStack = contextStack;
  }

  /// <summary>
  /// Converts a Rhino 3D point to a Speckle point.
  /// </summary>
  /// <param name="target">The Rhino 3D point to convert.</param>
  /// <returns>The converted Speckle point.</returns>
  public SOG.Point RawConvert(RG.Point3d target) =>
    new(target.X, target.Y, target.Z, _contextStack.Current.SpeckleUnits);

  public SOG.Point RawConvert(RG.Point target) => RawConvert(target.Location);
}
