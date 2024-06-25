using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Rhino7.Interfaces;

namespace Speckle.Converters.Rhino7.ToSpeckle.Raw;

public class VectorToSpeckleConverter : ITypedConverter<IRhinoVector3d, SOG.Vector>
{
  private readonly IConversionContextStack<IRhinoDoc, RhinoUnitSystem> _contextStack;

  public VectorToSpeckleConverter(IConversionContextStack<IRhinoDoc, RhinoUnitSystem> contextStack)
  {
    _contextStack = contextStack;
  }

  /// <summary>
  /// Converts a Rhino Vector3d object to a Speckle Vector object.
  /// </summary>
  /// <param name="target">The Rhino Vector3d object to convert.</param>
  /// <returns>The converted Speckle Vector object.</returns>
  public SOG.Vector Convert(IRhinoVector3d target) =>
    new(target.X, target.Y, target.Z, _contextStack.Current.SpeckleUnits);
}
