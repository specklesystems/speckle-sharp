using ArcGIS.Desktop.Mapping;

namespace Speckle.Connectors.ArcGIS.Utils;

public struct ObjectID
{
  private const string FEATURE_ID_SEPARATOR = "__speckleFeatureId__";
  public string MappedLayerURI { get; }
  public int? FeatureId { get; }
  public MapMember? MapMember { get; set; }

  public ObjectID(string encodedId)
  {
    List<string> stringParts = encodedId.Split(FEATURE_ID_SEPARATOR).ToList();
    MappedLayerURI = stringParts[0];
    FeatureId = null;
    if (stringParts.Count > 1)
    {
      FeatureId = Convert.ToInt32(stringParts[1]);
    }
  }

  public ObjectID(string layerId, int? featureId, MapMember? mapMember)
  {
    MappedLayerURI = layerId;
    FeatureId = featureId;
    MapMember = mapMember;
  }

  public readonly string ObjectIdToString()
  {
    if (FeatureId == null)
    {
      return $"{MappedLayerURI}";
    }
    else
    {
      return $"{MappedLayerURI}{FEATURE_ID_SEPARATOR}{FeatureId}";
    }
  }
}
