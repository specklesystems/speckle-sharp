using ArcGIS.Desktop.Mapping;

namespace Speckle.Connectors.ArcGIS.Utils;

// bind together a layer object on the map, and auto-assigned ID if the specific feature 
public readonly struct MapMemberFeature
{
  public int? FeatureId { get; } // unique feature id (start from 0) of a feature in the layer
  public MapMember MapMember { get; } // layer object on the Map

  public MapMemberFeature(MapMember mapMember, int? featureId)
  {
    MapMember = mapMember;
    FeatureId = featureId;
  }

}
