using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB.ExtensibleStorage;
using Newtonsoft.Json;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Speckle.DesktopUI.Utils;

namespace Speckle.ConnectorRevit.Storage
{
  /// <summary>
  /// Revit schema of the StreamBoxesWrapper class.
  /// </summary>
  public static class StreamBoxesSchema
  {
    static readonly Guid schemaGuid = new Guid("{F29ABD4E-C2DA-4F6A-A301-C70F1C32128D}");

    public static Schema GetSchema()
    {
      var schema = Schema.Lookup(schemaGuid);
      if (schema != null)
        return schema;

      var builder = new SchemaBuilder(schemaGuid);
      builder.SetSchemaName("StreamBoxesWrapper");
      builder.AddArrayField("streamBoxes", typeof(string));

      return builder.Finish();
    }
  }

  /// <summary>
  /// Unique schema for... something ¯\_(ツ)_/¯
  /// </summary>
  static class DSUniqueSchemaStreamBoxStorage
  {
    static readonly Guid schemaGuid = new Guid("{174C7EEE-EC5E-4A3F-894A-C801871AEDB8}");

    public static Schema GetSchema()
    {
      Schema schema = Schema.Lookup(schemaGuid);

      if (schema != null)
        return schema;

      SchemaBuilder schemaBuilder = new SchemaBuilder(schemaGuid);

      schemaBuilder.SetSchemaName("DataStorageUniqueId");

      schemaBuilder.AddSimpleField("Id", typeof(Guid));

      return schemaBuilder.Finish();
    }
  }
}
