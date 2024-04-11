using Speckle.Converters.Common.Objects;
using Speckle.Converters.Common;
using Speckle.Core.Models;
using Autodesk.AutoCAD.DatabaseServices;
using Speckle.Core.Kits;

namespace Speckle.Converters.Autocad.ToHost.Raw;

public class PointToHostRawConverter : IRawConversion<SOG.Point, AG.Point3d>
{
  private readonly IConversionContextStack<Document, UnitsValue> _contextStack;

  public PointToHostRawConverter(IConversionContextStack<Document, UnitsValue> contextStack)
  {
    _contextStack = contextStack;
  }

  public object Convert(Base target) => RawConvert((SOG.Point)target);

  public AG.Point3d RawConvert(SOG.Point target)
  {
    double f = Units.GetConversionFactor(target.units, _contextStack.Current.SpeckleUnits);
    AG.Point3d point = new(target.x * f, target.y * f, target.z * f);

    return point;
  }
}
