using ArcGIS.Core.Data;
using Objects.GIS;
using FieldDescription = ArcGIS.Core.Data.DDL.FieldDescription;

namespace Speckle.Converters.ArcGIS3.Utils;

public interface IArcGISFieldUtils
{
  public RowBuffer AssignFieldValuesToRow(RowBuffer rowBuffer, List<FieldDescription> fields, GisFeature feat);
  public List<FieldDescription> GetFieldsFromSpeckleLayer(VectorLayer target);
}
