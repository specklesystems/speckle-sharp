using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Kits;

namespace Speckle.Converters.Autocad.ToHost.Raw;

public class PointToHostRawConverter : ITypedConverter<SOG.Point, AG.Point3d>
{
  private readonly IConversionContextStack<Document, ADB.UnitsValue> _contextStack;

  public PointToHostRawConverter(IConversionContextStack<Document, ADB.UnitsValue> contextStack)
  {
    _contextStack = contextStack;
  }

  public AG.Point3d RawConvert(SOG.Point target)
  {
    double f = Units.GetConversionFactor(target.units, _contextStack.Current.SpeckleUnits);
    AG.Point3d point = new(target.x * f, target.y * f, target.z * f);
    return point;
  }
}
