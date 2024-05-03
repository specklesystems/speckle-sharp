using System;
using Autodesk.Revit.DB.ExtensibleStorage;

namespace Speckle.Connectors.Revit.HostApp;

public class IdStorageSchema : IStorageSchema
{
  private readonly Guid _schemaGuid = new("D0E2AD18-0DE0-41CF-A2B7-5384267061D7");

  public Schema GetSchema()
  {
    Schema schema = Schema.Lookup(_schemaGuid);
    if (schema != null)
    {
      return schema;
    }

    using SchemaBuilder builder = new(_schemaGuid);
    builder.SetSchemaName("DataStorageUniqueId");
    builder.AddSimpleField("Id", typeof(Guid));
    return builder.Finish();
  }
}
