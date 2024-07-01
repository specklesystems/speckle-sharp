using ArcGIS.Core.Data;
using ArcGIS.Core.Data.DDL;
using ArcGIS.Core.Data.Exceptions;
using Objects.GIS;
using Speckle.Converters.ArcGIS3.Utils;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;
using FieldDescription = ArcGIS.Core.Data.DDL.FieldDescription;

namespace Speckle.Converters.ArcGIS3.ToHost.Raw;

public class FeatureClassToHostConverter : ITypedConverter<VectorLayer, FeatureClass>
{
  private readonly ITypedConverter<IReadOnlyList<Base>, ACG.Geometry> _gisGeometryConverter;
  private readonly IFeatureClassUtils _featureClassUtils;
  private readonly IArcGISFieldUtils _fieldsUtils;
  private readonly IConversionContextStack<ArcGISDocument, ACG.Unit> _contextStack;

  public FeatureClassToHostConverter(
    ITypedConverter<IReadOnlyList<Base>, ACG.Geometry> gisGeometryConverter,
    IFeatureClassUtils featureClassUtils,
    IArcGISFieldUtils fieldsUtils,
    IConversionContextStack<ArcGISDocument, ACG.Unit> contextStack
  )
  {
    _gisGeometryConverter = gisGeometryConverter;
    _featureClassUtils = featureClassUtils;
    _fieldsUtils = fieldsUtils;
    _contextStack = contextStack;
  }

  private List<GisFeature> RecoverOutdatedGisFeatures(VectorLayer target)
  {
    List<GisFeature> gisFeatures = new();
    foreach (Base baseElement in target.elements)
    {
      if (baseElement is GisFeature feature)
      {
        gisFeatures.Add(feature);
      }
      else
      {
        if (
          baseElement["geometry"] is List<object> originalGeometries
          && baseElement["attributes"] is Base originalAttrs
          && (baseElement["displayValue"] is List<object> || baseElement["displayValue"] == null)
        )
        {
          var originalDisplayVal = baseElement["displayValue"];
          List<Base> geometry = originalGeometries.Select(x => (Base)x).ToList();
          Base attributes = originalAttrs;
          List<Base>? displayValue =
            originalDisplayVal == null
              ? new List<Base>()
              : ((List<object>)originalDisplayVal).Select(x => (Base)x).ToList();
          GisFeature newfeature =
            new()
            {
              geometry = geometry,
              attributes = attributes,
              displayValue = displayValue
            };
          gisFeatures.Add(newfeature);
        }
        else
        {
          gisFeatures.Add((GisFeature)baseElement);
        }
      }
    }
    return gisFeatures;
  }

  public FeatureClass Convert(VectorLayer target)
  {
    ACG.GeometryType geomType = _featureClassUtils.GetLayerGeometryType(target);

    FileGeodatabaseConnectionPath fileGeodatabaseConnectionPath =
      new(_contextStack.Current.Document.SpeckleDatabasePath);
    Geodatabase geodatabase = new(fileGeodatabaseConnectionPath);
    SchemaBuilder schemaBuilder = new(geodatabase);

    // create Spatial Reference (i.e. Coordinate Reference System - CRS)
    string wktString = string.Empty;
    if (target.crs is not null && target.crs.wkt is not null)
    {
      wktString = target.crs.wkt.ToString();
    }
    // ATM, GIS commit CRS is stored per layer, but should be moved to the Root level too, and created once per Receive
    ACG.SpatialReference spatialRef = ACG.SpatialReferenceBuilder.CreateSpatialReference(wktString);
    _contextStack.Current.Document.ActiveCRSoffsetRotation = new CRSoffsetRotation(spatialRef);

    // create Fields
    List<FieldDescription> fields = _fieldsUtils.GetFieldsFromSpeckleLayer(target);

    // getting rid of forbidden symbols in the class name: adding a letter in the beginning
    // https://pro.arcgis.com/en/pro-app/3.1/tool-reference/tool-errors-and-warnings/001001-010000/tool-errors-and-warnings-00001-00025-000020.htm
    string featureClassName = "speckleID_" + target.id;

    // delete FeatureClass if already exists
    foreach (FeatureClassDefinition fClassDefinition in geodatabase.GetDefinitions<FeatureClassDefinition>())
    {
      // will cause GeodatabaseCatalogDatasetException if doesn't exist in the database
      if (fClassDefinition.GetName() == featureClassName)
      {
        FeatureClassDescription existingDescription = new(fClassDefinition);
        schemaBuilder.Delete(existingDescription);
        schemaBuilder.Build();
      }
    }

    // Create FeatureClass
    try
    {
      // POC: make sure class has a valid crs
      ShapeDescription shpDescription = new(geomType, spatialRef) { HasZ = true };
      FeatureClassDescription featureClassDescription = new(featureClassName, fields, shpDescription);
      FeatureClassToken featureClassToken = schemaBuilder.Create(featureClassDescription);
    }
    catch (ArgumentException)
    {
      // POC: review the exception
      // if name has invalid characters/combinations
      throw;
    }
    bool buildStatus = schemaBuilder.Build();
    if (!buildStatus)
    {
      // POC: log somewhere the error in building the feature class
      IReadOnlyList<string> errors = schemaBuilder.ErrorMessages;
    }

    try
    {
      FeatureClass newFeatureClass = geodatabase.OpenDataset<FeatureClass>(featureClassName);
      // backwards compatibility:
      List<GisFeature> gisFeatures = RecoverOutdatedGisFeatures(target);
      // Add features to the FeatureClass
      geodatabase.ApplyEdits(() =>
      {
        _featureClassUtils.AddFeaturesToFeatureClass(newFeatureClass, gisFeatures, fields, _gisGeometryConverter);
      });

      return newFeatureClass;
    }
    catch (GeodatabaseException)
    {
      // POC: review the exception
      throw;
    }
  }
}
