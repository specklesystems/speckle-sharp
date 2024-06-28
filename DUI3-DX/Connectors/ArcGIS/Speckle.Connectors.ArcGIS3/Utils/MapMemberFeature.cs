using ArcGIS.Desktop.Mapping;

namespace Speckle.Connectors.ArcGIS.Utils;

public readonly struct MapMemberFeature
{
  public int? FeatureId { get; }
  public MapMember MapMember { get; }

  public MapMemberFeature(MapMember mapMember, int? featureId)
  {
    MapMember = mapMember;
    FeatureId = featureId;
  }

}
