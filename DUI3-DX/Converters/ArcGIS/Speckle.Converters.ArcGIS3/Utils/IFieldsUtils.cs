using ArcGIS.Core.Data;
using Objects.GIS;
using FieldDescription = ArcGIS.Core.Data.DDL.FieldDescription;

namespace Speckle.Converters.ArcGIS3.Utils;

public interface IFieldsUtils
{
  public RowBuffer AssignFieldValuesToRow(RowBuffer rowBuffer, List<FieldDescription> fields, GisFeature feat);
  public object? FieldValueToNativeType(FieldType fieldType, object? value);
  public List<FieldDescription> GetFieldsFromSpeckleLayer(VectorLayer target);
  public FieldType GetFieldTypeFromInt(int fieldType);
}
