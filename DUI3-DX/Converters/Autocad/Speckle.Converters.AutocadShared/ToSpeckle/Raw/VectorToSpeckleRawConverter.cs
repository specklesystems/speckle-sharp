using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.Autocad.ToSpeckle.Raw;

public class VectorToSpeckleRawConverter : IRawConversion<AG.Vector3d, SOG.Vector>
{
  private readonly IConversionContextStack<Document, ADB.UnitsValue> _contextStack;

  public VectorToSpeckleRawConverter(IConversionContextStack<Document, ADB.UnitsValue> contextStack)
  {
    _contextStack = contextStack;
  }

  public SOG.Vector RawConvert(AG.Vector3d target) =>
    new(target.X, target.Y, target.Z, _contextStack.Current.SpeckleUnits);
}
