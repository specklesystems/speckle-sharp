using System;
using Autodesk.Revit.DB.ExtensibleStorage;

namespace Speckle.ConnectorRevitDUI3.Utils;

static class DocumentModelStoreSchema
{
  private static readonly Guid SchemaGuid = new("D690F2B4-BDB0-4CB4-8657-17844ADF42AA");

  public static Schema GetSchema()
  {
    var schema = Schema.Lookup(SchemaGuid);
    if (schema != null)
    {
      return schema;
    }

    using var builder = new SchemaBuilder(SchemaGuid);
    builder.SetSchemaName("DUI3State");
    builder.AddSimpleField("contents", typeof(string));
    return builder.Finish();
  }
}

static class IdStorageSchema
{
  private static readonly Guid SchemaGuid = new("D0E2AD18-0DE0-41CF-A2B7-5384267061D7");

  public static Schema GetSchema()
  {
    var schema = Schema.Lookup(SchemaGuid);
    if (schema != null)
    {
      return schema;
    }

    using var builder = new SchemaBuilder(SchemaGuid);
    builder.SetSchemaName("DataStorageUniqueId");
    builder.AddSimpleField("Id", typeof(Guid));
    return builder.Finish();
  }
}
