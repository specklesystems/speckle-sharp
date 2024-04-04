using Speckle.Converters.Common.Objects;
using Speckle.Converters.Common;
using Speckle.Core.Models;
using Autodesk.AutoCAD.DatabaseServices;
using Speckle.Core.Kits;

namespace Speckle.Converters.Autocad.Geometry;

[NameAndRankValue(nameof(SOG.Point), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class DBPointToHostConverter : ISpeckleObjectToHostConversion, IRawConversion<SOG.Point, DBPoint>
{
  private readonly IConversionContextStack<Document, UnitsValue> _contextStack;

  public DBPointToHostConverter(IConversionContextStack<Document, UnitsValue> contextStack)
  {
    _contextStack = contextStack;
  }

  public object Convert(Base target) => RawConvert((SOG.Point)target);

  public DBPoint RawConvert(SOG.Point target)
  {
    double f = Units.GetConversionFactor(target.units, _contextStack.Current.SpeckleUnits);
    AG.Point3d point = new(target.x * f, target.y * f, target.z * f);
    return new(point);
  }
}

/* POC: enable this when converter registration is resolved
public class PointToHostConverter : IRawConversion<SOG.Point, AG.Point3d>
{
  private readonly IConversionContextStack<Document, UnitsValue> _contextStack;

  public PointToHostConverter(IConversionContextStack<Document, UnitsValue> contextStack)
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
*/
