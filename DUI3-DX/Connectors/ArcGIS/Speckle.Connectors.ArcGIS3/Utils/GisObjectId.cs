using ArcGIS.Desktop.Mapping;

namespace Speckle.Connectors.ArcGIS.Utils;

public readonly struct GisObjectId
{
  public const char FORMAT_SEPARATOR = '%';
  public string LayerUri { get; }
  public int? FeatureIndex { get; }

  public GisObjectId(string layerUri, int? featureIndex = null)
  {
    LayerUri = layerUri;
    FeatureIndex = featureIndex;
  }

  public static GisObjectId FromEncodedString(string encodedString)
  {
    string[] parts = encodedString.Split(FORMAT_SEPARATOR);
    if (parts.Length != 2)
    {
      throw new FormatException($"{encodedString} was a valid encoded string");
    }

    string layerUri = parts[0];
    int? featureIndex = null;
    if (!int.TryParse(parts[1], out int index))
    {
      featureIndex = index;
    }

    return new GisObjectId(layerUri, featureIndex);
  }

  public override string ToString() => $"{LayerUri}{FORMAT_SEPARATOR}{FeatureIndex}";

  public static void GetObject(Map map)
  {
    map.FindLayer("CIMPATH=map/u_s__states__generalized_.xml");
  }
}
