using System;
using System.Collections.Generic;

using Autodesk.Revit.DB;
using Autodesk.Revit.DB.ExtensibleStorage;
using DesktopUI2.Models;
using Speckle.Newtonsoft.Json;

namespace Speckle.ConnectorRevit.Storage
{
  /// <summary>
  /// Manages the serialisation of speckle stream state
  /// (stream info, account info, and filter type) in a revit document.
  /// </summary>
  public static class StreamStateManager
  {
    readonly static Guid ID = new Guid("4EF264B9-5AA0-4B99-A6E7-C82ACEB26DE2");

    /// <summary>
    /// Returns all the speckle stream states present in the current document.
    /// </summary>
    /// <param name="doc"></param>
    /// <returns></returns>
    public static List<StreamState> ReadState(Document doc)
    {
      try
      {
        var streamStatesEntity = GetSpeckleEntity(doc);
        if (streamStatesEntity == null || !streamStatesEntity.IsValid())
          return new List<StreamState>();

        var str = streamStatesEntity.Get<string>("StreamStates");
        var states = JsonConvert.DeserializeObject<List<StreamState>>(str);

        return states;
      }
      catch (Exception e)
      {
        return new List<StreamState>();
      }
    }

    /// <summary>
    /// Writes the stream states to the current document.
    /// </summary>
    /// <param name="doc"></param>
    /// <param name="wrap"></param>
    public static void WriteStreamStateList(Document doc, List<StreamState> streamStates)
    {
      var ds = GetSettingsDataStorage(doc);

      if (ds == null)
        ds = DataStorage.Create(doc);

      var streamStatesEntity = new Entity(StreamStateListSchema2.GetSchema());

      streamStatesEntity.Set("StreamStates", JsonConvert.SerializeObject(streamStates) as string);

      var idEntity = new Entity(DSUniqueSchemaStreamStateStorage2.GetSchema());
      idEntity.Set("Id", ID);

      ds.SetEntity(idEntity);
      ds.SetEntity(streamStatesEntity);
    }

    private static DataStorage GetSettingsDataStorage(Document doc)
    {
      // Retrieve all data storages from project
      var collector = new FilteredElementCollector(doc);

      var dataStorages = collector.OfClass(typeof(DataStorage));

      // Find setting data storage
      foreach (DataStorage dataStorage in dataStorages)
      {
        var settingIdEntity = dataStorage.GetEntity(DSUniqueSchemaStreamStateStorage2.GetSchema());

        if (!settingIdEntity.IsValid())
          continue;

        var id = settingIdEntity.Get<Guid>("Id");

        if (!id.Equals(ID))
          continue;

        return dataStorage;
      }
      return null;
    }

    private static Entity GetSpeckleEntity(Document doc)
    {
      FilteredElementCollector collector = new FilteredElementCollector(doc);

      var dataStorages = collector.OfClass(typeof(DataStorage));
      foreach (DataStorage dataStorage in dataStorages)
      {
        Entity settingEntity = dataStorage.GetEntity(StreamStateListSchema2.GetSchema());
        if (!settingEntity.IsValid())
          continue;

        return settingEntity;
      }
      return null;
    }
  }

  /// <summary>
  /// Revit schema of the StreamStateWrapper class.
  /// </summary>
  public static class StreamStateListSchema2
  {
    static readonly Guid schemaGuid = new Guid("C48D05AE-8068-4B9A-A790-B4B2F605126B");

    public static Schema GetSchema()
    {
      var schema = Schema.Lookup(schemaGuid);
      if (schema != null)
        return schema;

      var builder = new SchemaBuilder(schemaGuid);
      builder.SetSchemaName("StreamStateWrapper");
      builder.AddSimpleField("StreamStates", typeof(string));

      return builder.Finish();
    }
  }


  /// <summary>
  /// Unique schema for... something ¯\_(ツ)_/¯
  /// </summary>
  static class DSUniqueSchemaStreamStateStorage2
  {
    static readonly Guid schemaGuid = new Guid("C0DA9F31-83A7-4775-807B-4430446E694F");

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
