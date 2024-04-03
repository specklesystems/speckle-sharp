using ArcGIS.Core.Geometry;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;
using Speckle.Converters.Common;
using ArcGIS.Desktop.Mapping;

namespace Speckle.Converters.ArcGIS3.Geometry;

[NameAndRankValue(nameof(Multipoint), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class MultipointToSpeckleConverter : IHostObjectToSpeckleConversion, IRawConversion<Multipoint, SOG.Point>
{
  private readonly IConversionContextStack<Map, Unit> _contextStack;

  public MultipointToSpeckleConverter(IConversionContextStack<Map, Unit> contextStack)
  {
    _contextStack = contextStack;
  }

  public Base Convert(object target) => RawConvert((Multipoint)target);

  public SOG.Point RawConvert(Multipoint target)
  {
    List<SOG.Point> multipoint = new();
    foreach (MapPoint point in target.Points)
    {
      multipoint.Add(new(point.X, point.Y, point.Z, _contextStack.Current.SpeckleUnits));
    }

    return multipoint[0];
  }
}
