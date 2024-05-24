using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Speckle.Converters.Autocad.ToHost.Raw;

public class VectorToHostRawConverter : ITypedConverter<SOG.Vector, AG.Vector3d>
{
  private readonly IConversionContextStack<Document, ADB.UnitsValue> _contextStack;

  public VectorToHostRawConverter(IConversionContextStack<Document, ADB.UnitsValue> contextStack)
  {
    _contextStack = contextStack;
  }

  public object Convert(Base target) => Convert((SOG.Vector)target);

  public AG.Vector3d Convert(SOG.Vector target)
  {
    double f = Units.GetConversionFactor(target.units, _contextStack.Current.SpeckleUnits);
    return new(target.x * f, target.y * f, target.z * f);
  }
}
