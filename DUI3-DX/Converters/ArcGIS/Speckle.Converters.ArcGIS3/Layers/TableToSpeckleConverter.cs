using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;
using Objects.GIS;
using Speckle.Converters.Common;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;

namespace Speckle.Converters.ArcGIS3.Layers;

[NameAndRankValue(nameof(StandaloneTable), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class StandaloneTableToSpeckleConverter
  : IHostObjectToSpeckleConversion,
    IRawConversion<StandaloneTable, VectorLayer>
{
  private readonly IRawConversion<Row, GisFeature> _gisFeatureConverter;
  private readonly IConversionContextStack<Map, Unit> _contextStack;

  public StandaloneTableToSpeckleConverter(
    IRawConversion<Row, GisFeature> gisFeatureConverter,
    IConversionContextStack<Map, Unit> contextStack
  )
  {
    _gisFeatureConverter = gisFeatureConverter;
    _contextStack = contextStack;
  }

  public Base Convert(object target)
  {
    return RawConvert((StandaloneTable)target);
  }

  public VectorLayer RawConvert(StandaloneTable target)
  {
    VectorLayer speckleLayer = new() { name = target.Name, };

    // get feature class fields
    var attributes = new Base();
    var dispayTable = target as IDisplayTable;
    var fieldDescriptions = dispayTable.GetFieldDescriptions();
    foreach (var field in fieldDescriptions)
    {
      if (field.IsVisible)
      {
        string name = field.Name;
        attributes[name] = (int)field.Type;
      }
    }

    speckleLayer.attributes = attributes;
    string spekleGeometryType = "None";

    using (RowCursor rowCursor = dispayTable.Search())
    {
      while (rowCursor.MoveNext())
      {
        // Same IDisposable issue appears to happen on Row class too. Docs say it should always be disposed of manually by the caller.
        using (Row row = rowCursor.Current)
        {
          GisFeature element = _gisFeatureConverter.RawConvert(row);

          // replace "attributes", to remove non-visible layer attributes
          Base elementAttributes = new();
          foreach (FieldDescription field in fieldDescriptions)
          {
            elementAttributes[field.Name] = element.attributes[field.Name];
          }
          element.attributes = elementAttributes;

          speckleLayer.elements.Add(element);
        }
      }
    }

    speckleLayer.geomType = spekleGeometryType;
    return speckleLayer;
  }
}
