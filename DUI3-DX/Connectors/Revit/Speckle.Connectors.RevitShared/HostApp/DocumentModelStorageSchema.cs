using Autodesk.Revit.DB.ExtensibleStorage;

namespace Speckle.Connectors.Revit.HostApp;

public class DocumentModelStorageSchema : IStorageSchema
{
  private readonly Guid _schemaGuid = new("D690F2B4-BDB0-4CB4-8657-17844ADF42AA");

  public Schema GetSchema()
  {
    Schema schema = Schema.Lookup(_schemaGuid);
    if (schema != null)
    {
      return schema;
    }

    using SchemaBuilder builder = new(_schemaGuid);
    builder.SetSchemaName("DUI3State");
    builder.AddSimpleField("contents", typeof(string));
    return builder.Finish();
  }
}
