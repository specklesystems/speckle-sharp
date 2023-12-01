using System;
using Autodesk.Revit.DB.ExtensibleStorage;

namespace Speckle.ConnectorRevitDUI3.Utils;

public static class DocumentModelStoreSchema
{
  private static readonly Guid s_schemaGuid = new("D690F2B4-BDB0-4CB4-8657-17844ADF42AA");

  public static Schema GetSchema()
  {
    Schema schema = Schema.Lookup(s_schemaGuid);
    if (schema != null)
    {
      return schema;
    }

    using SchemaBuilder builder = new(s_schemaGuid);
    builder.SetSchemaName("DUI3State");
    builder.AddSimpleField("contents", typeof(string));
    return builder.Finish();
  }
}

public static class IdStorageSchema
{
  private static readonly Guid s_schemaGuid = new("D0E2AD18-0DE0-41CF-A2B7-5384267061D7");

  public static Schema GetSchema()
  {
    Schema schema = Schema.Lookup(s_schemaGuid);
    if (schema != null)
    {
      return schema;
    }

    using SchemaBuilder builder = new(s_schemaGuid);
    builder.SetSchemaName("DataStorageUniqueId");
    builder.AddSimpleField("Id", typeof(Guid));
    return builder.Finish();
  }
}
