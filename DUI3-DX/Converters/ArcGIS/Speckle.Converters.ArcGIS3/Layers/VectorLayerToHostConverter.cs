using ArcGIS.Core.Data;
using ArcGIS.Core.Data.DDL;
using ArcGIS.Core.Data.Exceptions;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using Objects.GIS;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;
using FieldDescription = ArcGIS.Core.Data.DDL.FieldDescription;

namespace Speckle.Converters.ArcGIS3.Layers;

[NameAndRankValue(nameof(VectorLayer), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class VectorLayerToHostConverter : ISpeckleObjectToHostConversion, IRawConversion<VectorLayer, Task<string>>
{
  private readonly IConversionContextStack<Map, Unit> _contextStack;
  private readonly IRawConversion<Base, ArcGIS.Core.Geometry.Geometry> _gisGeometryConverter;

  public VectorLayerToHostConverter(
    IConversionContextStack<Map, Unit> contextStack,
    IRawConversion<Base, ArcGIS.Core.Geometry.Geometry> gisGeometryConverter
  )
  {
    _contextStack = contextStack;
    _gisGeometryConverter = gisGeometryConverter;
  }

  public object Convert(Base target) => RawConvert((VectorLayer)target);

  public Task<string> RawConvert(VectorLayer target)
  {
    string message = string.Empty;
    string fGdbPath = string.Empty;
    try
    {
      return QueuedTask.Run(() =>
      {
        // Use Speckle geodatabase
        try
        {
          var parentDirectory = Directory.GetParent(Project.Current.URI);
          if (parentDirectory == null)
          {
            throw new ArgumentException($"Project directory {Project.Current.URI} not found");
          }
          fGdbPath = parentDirectory.ToString();
        }
        catch (Exception e)
        {
          throw;
        }

        var fGdbName = "Speckle.gdb";
        FileGeodatabaseConnectionPath fileGeodatabaseConnectionPath = new(new Uri(fGdbPath + "\\" + fGdbName));
        Geodatabase geodatabase = new(fileGeodatabaseConnectionPath);
        SchemaBuilder schemaBuilder = new(geodatabase);

        // https://pro.arcgis.com/en/pro-app/3.1/tool-reference/tool-errors-and-warnings/001001-010000/tool-errors-and-warnings-00001-00025-000020.htm
        string featureClassName = $"{target.id}___{target.name.Replace(" ", "_").Replace("%", "_").Replace("*", "_")}";

        string wktString = string.Empty;
        if (target.crs is not null && target.crs.wkt is not null)
        {
          wktString = target.crs.wkt.ToString();
        }
        SpatialReference spatialRef = SpatialReferenceBuilder.CreateSpatialReference(wktString);

        GeometryType geomType = new();
        if (target.nativeGeomType == null)
        {
          throw new SpeckleConversionException($"Unknown geometry type for layer {target.name}");
        }
        else
        {
          if (target.nativeGeomType.ToLower().Contains("point"))
          {
            geomType = GeometryType.Multipoint;
          }
          else if (target.nativeGeomType.ToLower().Contains("polyline"))
          {
            geomType = GeometryType.Polyline;
          }
          else if (target.nativeGeomType.ToLower().Contains("polygon"))
          {
            geomType = GeometryType.Polygon;
          }
          else if (target.nativeGeomType.ToLower().Contains("multipatch"))
          {
            geomType = GeometryType.Multipatch;
          }
          // throw
        }

        // Create FeatureClass
        List<FieldDescription> fields = new();
        List<string> fieldAdded = new();
        foreach (var field in target.attributes.GetMembers(DynamicBaseMemberType.Dynamic))
        {
          if (!fieldAdded.Contains(field.Key) && field.Key != "OBJECTID")
          {
            // TODO: choose the right type for Field
            // TODO check for the frbidden characters/combinations: https://support.esri.com/en-us/knowledge-base/what-characters-should-not-be-used-in-arcgis-for-field--000005588

            // fields.Add(new FieldDescription(field, FieldType.Integer));
            fields.Add(FieldDescription.CreateStringField(field.Key, 255)); // (int)(long)target.attributes[field.Value]));
            fieldAdded.Add(field.Key);
          }
        }
        try
        {
          FeatureClassDescription featureClassDescription =
            new(featureClassName, fields, new ShapeDescription(geomType, spatialRef));
          FeatureClassToken featureClassToken = schemaBuilder.Create(featureClassDescription);
        }
        catch (ArgumentException ex)
        {
          throw new ArgumentException($"{ex.Message}: {featureClassName}");
        }

        bool buildStatus = schemaBuilder.Build();
        if (!buildStatus)
        {
          IReadOnlyList<string> errors = schemaBuilder.ErrorMessages;
        }

        // Add features to the FeatureClass
        FeatureClass newFeatureClass = geodatabase.OpenDataset<FeatureClass>(featureClassName);
        // TODO: repeat for other geometry types
        if (geomType == GeometryType.Multipoint)
        {
          geodatabase.ApplyEdits(() =>
          {
            newFeatureClass.DeleteRows(new QueryFilter());
            foreach (GisFeature feat in target.elements.Cast<GisFeature>())
            {
              using (RowBuffer rowBuffer = newFeatureClass.CreateRowBuffer())
              {
                foreach (string field in fieldAdded)
                {
                  // try to assign values to writeable fields
                  try
                  {
                    if (feat.attributes is not null)
                    {
                      var value = feat.attributes[field];
                      if (value is not null)
                      {
                        rowBuffer[field] = value.ToString();
                      }
                      else
                      {
                        rowBuffer[field] = null;
                      }
                    }
                  }
                  catch (GeodatabaseFieldException)
                  {
                    // non-editable Field, do nothing
                  }
                }

                if (feat.geometry != null)
                {
                  foreach (var geometryPart in feat.geometry)
                  {
                    // TODO: repeat for all geometries, add as Multipart
                    ArcGIS.Core.Geometry.Geometry nativeShape = _gisGeometryConverter.RawConvert(geometryPart);
                    rowBuffer[newFeatureClass.GetDefinition().GetShapeField()] = nativeShape;
                    break;
                  }
                }
                newFeatureClass.CreateRow(rowBuffer).Dispose();
              }
            }
          });
        }
        return featureClassName;
      });
    }
    catch (GeodatabaseException exObj)
    {
      throw new InvalidOperationException($"Something went wrong: {exObj.Message}");
    }
  }
}
