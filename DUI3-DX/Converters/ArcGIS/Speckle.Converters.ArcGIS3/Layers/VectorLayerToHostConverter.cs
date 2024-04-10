using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Mapping;
using Objects.GIS;
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
    /*
    // Use Speckle geodatabase
    var fGdbPath = Directory.GetParent(Project.Current.URI).ToString();
    var fGdbName = "Speckle5.gdb";
    FileGeodatabaseConnectionPath fileGeodatabaseConnectionPath = new FileGeodatabaseConnectionPath(
      new Uri(fGdbPath + "\\" + fGdbName)
    );
    Geodatabase geodatabase = SchemaBuilder.CreateGeodatabase(fileGeodatabaseConnectionPath);

    ////////////////////// https://pro.arcgis.com/en/pro-app/3.1/sdk/api-reference/topic40923.html
    string featureDatasetName = "featureDatasetName";
    string featureClassName = target.name;

    SchemaBuilder schemaBuilder = new SchemaBuilder(geodatabase);

    // Create a FeatureDataset token
    FeatureDatasetDescription featureDatasetDescription = new FeatureDatasetDescription(
      featureDatasetName,
      SpatialReferences.WGS84
    );
    FeatureDatasetToken featureDatasetToken = schemaBuilder.Create(featureDatasetDescription);

    // Create a FeatureClass description
    FeatureClassDescription featureClassDescription = new FeatureClassDescription(
      featureClassName,
      new List<FieldDescription>()
      {
        new FieldDescription("Id", FieldType.Integer),
        new FieldDescription("Address", FieldType.String)
      },
      new ShapeDescription(GeometryType.Point, SpatialReferences.WGS84)
    );

    // Create a FeatureClass inside a FeatureDataset
    FeatureClassToken featureClassToken = schemaBuilder.Create(
      new FeatureDatasetDescription(featureDatasetToken),
      featureClassDescription
    );
    // Build status
    bool buildStatus = schemaBuilder.Build();
    // Build errors
    if (!buildStatus)
    {
      IReadOnlyList<string> errors = schemaBuilder.ErrorMessages;
    }
    */
    // POC: Bake here converted objects into ArcGIS Map.
    // POC: add here baked arcgis objects into list that we will return

    return null;
  }
}
