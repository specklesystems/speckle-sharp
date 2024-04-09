using Rhino;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.Rhino7.ToSpeckle.Raw;

// POC: not sure I like the place of the default rank
public class PointToSpeckleConverter : IRawConversion<RG.Point3d, SOG.Point>
{
  private readonly IConversionContextStack<RhinoDoc, UnitSystem> _contextStack;

  public PointToSpeckleConverter(IConversionContextStack<RhinoDoc, UnitSystem> contextStack)
  {
    _contextStack = contextStack;
  }

  public SOG.Point RawConvert(RG.Point3d target) =>
    new(target.X, target.Y, target.Z, _contextStack.Current.SpeckleUnits);
}
