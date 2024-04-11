using Speckle.Converters.Common.Objects;
using Speckle.Converters.Common;
using Speckle.Core.Models;
using Autodesk.AutoCAD.DatabaseServices;

namespace Speckle.Converters.Autocad.ToSpeckle.Geometry;

[NameAndRankValue(nameof(DBPoint), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class PointToSpeckleConverter : IHostObjectToSpeckleConversion
{
  private readonly IRawConversion<AG.Point3d, SOG.Point> _pointConverter;
  private readonly IConversionContextStack<Document, UnitsValue> _contextStack;

  public PointToSpeckleConverter(
    IRawConversion<AG.Point3d, SOG.Point> pointConverter,
    IConversionContextStack<Document, UnitsValue> contextStack
  )
  {
    _pointConverter = pointConverter;
    _contextStack = contextStack;
  }

  public Base Convert(object target) => RawConvert((DBPoint)target);

  public SOG.Point RawConvert(DBPoint target) => _pointConverter.RawConvert(target.Position);
}
