using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB.ExtensibleStorage;
using Newtonsoft.Json;

namespace Speckle.ConnectorRevit.Storage
{
  /// <summary>
  /// Wrapper class to manage the storage of speckle clients.
  /// </summary>
  public class SpeckleClientsWrapper
  {
    public List<dynamic> clients { get; set; }

    public SpeckleClientsWrapper() { clients = new List<dynamic>(); }

    public List<string> GetStringList()
    {
      var myList = new List<string>();
      foreach (dynamic el in clients)
      {
        myList.Add(JsonConvert.SerializeObject(el));
      }
      return myList;
    }

    public void SetClients(IList<string> stringList)
    {
      clients = stringList.Select(el => JsonConvert.DeserializeObject<dynamic>(el)).ToList();
    }
  }

  /// <summary>
  /// Revit schema of the SpeckleClientWrapper class. 
  /// </summary>
  public static class SpeckleClientsSchema
  {
    readonly static Guid schemaGuid = new Guid("{F29ABD4E-C2DA-4F6A-A301-C70F1C32128D}");

    public static Schema GetSchema()
    {
      var schema = Schema.Lookup(schemaGuid);
      if (schema != null) return schema;

      var builder = new SchemaBuilder(schemaGuid);
      builder.SetSchemaName("SpeckleClientStorage");
      builder.AddArrayField("clients", typeof(string));

      return builder.Finish();
    }
  }

  /// <summary>
  /// Unique schema for... something ¯\_(ツ)_/¯
  /// </summary>
  static class DSUniqueSchemaClientsStorage
  {
    static readonly Guid schemaGuid = new Guid("{174C7EEE-EC5E-4A3F-894A-C801871AEDB8}");

    public static Schema GetSchema()
    {
      Schema schema = Schema.Lookup(schemaGuid);

      if (schema != null) return schema;

      SchemaBuilder schemaBuilder = new SchemaBuilder(schemaGuid);

      schemaBuilder.SetSchemaName("DataStorageUniqueId");

      schemaBuilder.AddSimpleField("Id", typeof(Guid));

      return schemaBuilder.Finish();
    }
  }
}
