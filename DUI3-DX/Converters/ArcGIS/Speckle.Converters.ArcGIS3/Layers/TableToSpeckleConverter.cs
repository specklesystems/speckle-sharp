using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;
using Objects.GIS;
using Speckle.Converters.Common;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Core.Data;

namespace Speckle.Converters.ArcGIS3.Layers;

[NameAndRankValue(nameof(StandaloneTable), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class StandaloneTableToSpeckleConverter
  : IToSpeckleTopLevelConverter,
    ITypedConverter<StandaloneTable, VectorLayer>
{
  private readonly ITypedConverter<Row, GisFeature> _gisFeatureConverter;

  public StandaloneTableToSpeckleConverter(ITypedConverter<Row, GisFeature> gisFeatureConverter)
  {
    _gisFeatureConverter = gisFeatureConverter;
  }

  public Base Convert(object target)
  {
    return Convert((StandaloneTable)target);
  }

  public VectorLayer Convert(StandaloneTable target)
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
          GisFeature element = _gisFeatureConverter.Convert(row);

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
