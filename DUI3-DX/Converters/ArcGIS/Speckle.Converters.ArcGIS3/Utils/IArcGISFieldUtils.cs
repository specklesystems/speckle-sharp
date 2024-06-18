using ArcGIS.Core.Data;
using Objects.GIS;
using Speckle.Core.Models;
using FieldDescription = ArcGIS.Core.Data.DDL.FieldDescription;

namespace Speckle.Converters.ArcGIS3.Utils;

public interface IArcGISFieldUtils
{
  public RowBuffer AssignFieldValuesToRow(
    RowBuffer rowBuffer,
    List<FieldDescription> fields,
    Dictionary<string, object?> attributes
  );
  public List<FieldDescription> GetFieldsFromSpeckleLayer(VectorLayer target);

  public List<(FieldDescription, Func<Base, object?>)> CreateFieldsFromListOfBase(List<Base> target);
}
