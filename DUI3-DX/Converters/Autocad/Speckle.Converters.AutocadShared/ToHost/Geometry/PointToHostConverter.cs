using Speckle.Converters.Common.Objects;
using Speckle.Converters.Common;
using Speckle.Core.Models;
using Autodesk.AutoCAD.DatabaseServices;
using Speckle.Core.Kits;

namespace Speckle.Converters.Autocad.ToHost.Geometry;

[NameAndRankValue(nameof(SOG.Point), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class PointToHostConverter : ISpeckleObjectToHostConversion
{
  private readonly IRawConversion<SOG.Point, AG.Point3d> _pointConverter;
  private readonly IConversionContextStack<Document, UnitsValue> _contextStack;

  public PointToHostConverter(
    IRawConversion<SOG.Point, AG.Point3d> pointConverter,
    IConversionContextStack<Document, UnitsValue> contextStack
  )
  {
    _pointConverter = pointConverter;
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
