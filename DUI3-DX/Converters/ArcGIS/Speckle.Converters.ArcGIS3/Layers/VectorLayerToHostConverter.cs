using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Mapping;
using Objects.GIS;
using Speckle.Converters.ArcGIS3.Utils;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.ArcGIS3.Layers;

[NameAndRankValue(nameof(VectorLayer), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class VectorLayerToHostConverter : ISpeckleObjectToHostConversion, IRawConversion<VectorLayer, object>
{
  private readonly IConversionContextStack<Map, Unit> _contextStack;

  public VectorLayerToHostConverter(IConversionContextStack<Map, Unit> contextStack)
  {
    _contextStack = contextStack;
  }

  public object Convert(Base target) => RawConvert((VectorLayer)target);

  public object RawConvert(VectorLayer target)
  {
    string projectPath = Project.Current.URI;
    string newGDBPath = projectPath.Replace(".aprx", "");
    var fGdbPath = Directory.GetParent(newGDBPath).ToString();
    var fGdbName = "Speckle.gdb";
    var utils = new ArcGISProjectUtils();
    Task task = utils.NewFileGDB(fGdbPath, fGdbName);
    var flyrCreatnParam = new FeatureLayerCreationParams(new Uri(fGdbPath + "\\" + fGdbName))
    {
      Name = "World Cities",
      IsVisible = true,
    };

    var featureLayer = LayerFactory.Instance.CreateLayer<FeatureLayer>(
      flyrCreatnParam,
      MapView.Active.Map
    // _contextStack.Current.Document
    );
    // POC: Bake here converted objects into ArcGIS Map.
    // POC: add here baked arcgis objects into list that we will return

    return null;
  }
}
