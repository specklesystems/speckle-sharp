using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.ExtensibleStorage;
using Newtonsoft.Json;
using Speckle.Core.Api;

namespace Speckle.ConnectorRevit.Storage
{
  public static class SpeckleStateSchema
  {
    readonly static Guid schemaGuid = new Guid("{E9998B36-E939-4892-A9AE-AA47CFC5C464}");

    public static Schema GetSchema()
    {
      var schema = Schema.Lookup(schemaGuid);
      if (schema != null)return schema;

      var builder = new SchemaBuilder(schemaGuid);
      builder.SetSchemaName("SpeckleLocalStateStorage");
      builder.AddArrayField("streams", typeof(string));

      return builder.Finish();
    }
  }

  static class DSUniqueSchemaLocalState
  {
    static readonly Guid schemaGuid = new Guid("{574C7937-5698-41F7-9B20-C4740158F22F}");

    public static Schema GetSchema()
    {
      Schema schema = Schema.Lookup(schemaGuid);

      if (schema != null)return schema;

      SchemaBuilder schemaBuilder = new SchemaBuilder(schemaGuid);

      schemaBuilder.SetSchemaName("DSUniqueSchemaLocalState");

      schemaBuilder.AddSimpleField("Id", typeof(Guid));

      return schemaBuilder.Finish();
    }
  }

  public static class SpeckleStateManager
  {
    readonly static Guid ID = new Guid("{6EA724BE-0500-4AB3-8288-125EAD712127}");

    public static List<Stream> ReadState(Autodesk.Revit.DB.Document doc)
    {
      var localStateEntity = GetStateEntity(doc);
      if (localStateEntity == null || !localStateEntity.IsValid())
        return new List<Stream>();

      var serState = localStateEntity.Get<IList<string>>("streams");
      var myState = serState.Select(str => JsonConvert.DeserializeObject<Stream>(str)).ToList();

      return myState != null ? myState : new List<Stream>();
    }

    public static void WriteState(Autodesk.Revit.DB.Document doc, List<Stream> state)
    {
      var ds = GetStateDataStorage(doc);

      // TODO: The problem: 
      // "Attempt to modify the model outside of transaction."
      // Needs to be wrapped in a Action and sloshed in the queue. 
      try
      {
        if (ds == null)ds = DataStorage.Create(doc);
      }
      catch (Exception e)
      {
        Debug.WriteLine($"Error in WriteState: {e}");
      }
      Entity speckleStateEntity = new Entity(SpeckleStateSchema.GetSchema());

      var ls = state.Select(stream => JsonConvert.SerializeObject(stream)).ToList();

      speckleStateEntity.Set("streams", ls as IList<string>);

      Entity idEntity = new Entity(DSUniqueSchemaLocalState.GetSchema());
      idEntity.Set("Id", ID);

      ds.SetEntity(idEntity);
      ds.SetEntity(speckleStateEntity);
    }

    private static DataStorage GetStateDataStorage(Autodesk.Revit.DB.Document doc)
    {
      // Retrieve all data storages from project
      FilteredElementCollector collector = new FilteredElementCollector(doc);

      var dataStorages = collector.OfClass(typeof(DataStorage));

      // Find setting data storage
      foreach (DataStorage dataStorage in dataStorages)
      {
        Entity settingIdEntity = dataStorage.GetEntity(DSUniqueSchemaLocalState.GetSchema());

        if (!settingIdEntity.IsValid())continue;

        var id = settingIdEntity.Get<Guid>("Id");

        if (!id.Equals(ID))continue;

        return dataStorage;
      }
      return null;
    }

    private static Entity GetStateEntity(Autodesk.Revit.DB.Document doc)
    {
      FilteredElementCollector collector = new FilteredElementCollector(doc);

      var dataStorages = collector.OfClass(typeof(DataStorage));
      foreach (DataStorage dataStorage in dataStorages)
      {
        Entity settingEntity = dataStorage.GetEntity(SpeckleStateSchema.GetSchema());
        if (!settingEntity.IsValid())continue;

        return settingEntity;
      }
      return null;
    }
  }
}
