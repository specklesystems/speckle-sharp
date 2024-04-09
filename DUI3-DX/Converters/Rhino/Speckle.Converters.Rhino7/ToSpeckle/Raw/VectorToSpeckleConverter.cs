using Rhino;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.Rhino7.ToSpeckle.Raw;

public class VectorToSpeckleConverter : IRawConversion<RG.Vector3d, SOG.Vector>
{
  private readonly IConversionContextStack<RhinoDoc, UnitSystem> _contextStack;

  public VectorToSpeckleConverter(IConversionContextStack<RhinoDoc, UnitSystem> contextStack)
  {
    _contextStack = contextStack;
  }

  public SOG.Vector RawConvert(RG.Vector3d target) =>
    new(target.X, target.Y, target.Z, _contextStack.Current.SpeckleUnits);
}
