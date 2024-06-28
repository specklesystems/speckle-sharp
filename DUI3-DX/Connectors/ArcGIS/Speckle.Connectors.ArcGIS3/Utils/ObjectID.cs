namespace Speckle.Connectors.ArcGIS.Utils;

// this struct is needed to be able to parse single-string value into IDs of both a layer, and it's individual feature
public struct ObjectID
{
  private const string FEATURE_ID_SEPARATOR = "__speckleFeatureId__";
  public string MappedLayerURI { get; } // unique ID of the layer on the map
  public int? FeatureId { get; } // unique feature id (start from 0) of a feature in the layer

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

  public ObjectID(string layerId, int? featureId)
  {
    MappedLayerURI = layerId;
    FeatureId = featureId;
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
