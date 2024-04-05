namespace Speckle.Converters.ArcGIS3.Features;
/*
[NameAndRankValue(nameof(MapPoint), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class PointToSpeckleConverter : IHostObjectToSpeckleConversion, IRawConversion<Multipoint, Point>
{
  private readonly IConversionContextStack<Map, Unit> _contextStack;

  public PointToSpeckleConverter(IConversionContextStack<Map, Unit> contextStack)
  {
    _contextStack = contextStack;
  }

  public Base Convert(object target) => RawConvert((MapPoint)target);

  public Point RawConvert(MapPoint target) => new(target.X, target.Y, target.Z, _contextStack.Current.SpeckleUnits);
}
*/
