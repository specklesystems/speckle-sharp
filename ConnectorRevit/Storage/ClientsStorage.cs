using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.ExtensibleStorage;
using Speckle.DesktopUI.Utils;

namespace Speckle.ConnectorRevit.Storage
{
  /// <summary>
  /// Manages the serialisation of speckle stream state
  /// (stream info, account info, and filter type) in a revit document.
  /// </summary>
  public static class StreamStateManager
  {
    readonly static Guid ID = new Guid("{5D453471-1F20-44CE-B1D0-BBD2BDE4616A}");

    /// <summary>
    /// Returns the speckle stream states present in the current document.
    /// </summary>
    /// <param name="doc"></param>
    /// <returns></returns>
    public static StreamStateWrapper ReadState(Document doc)
    {
      var streamStatesEntity = GetSpeckleEntity(doc);
      if (streamStatesEntity == null || !streamStatesEntity.IsValid())
        return null;

      var myStreamStates = new StreamStateWrapper();
      myStreamStates.SetState(streamStatesEntity.Get<IList<string>>("StreamStates"));

      return myStreamStates;
    }

    /// <summary>
    /// Writes the stream states to the current document.
    /// </summary>
    /// <param name="doc"></param>
    /// <param name="wrap"></param>
    public static void WriteState(Document doc, StreamStateWrapper wrap)
    {
      var ds = GetSettingsDataStorage(doc);

      if (ds == null)
        ds = DataStorage.Create(doc);

      var streamStatesEntity = new Entity(StreamStateSchema.GetSchema());

      streamStatesEntity.Set("StreamStates", wrap.GetStringList()as IList<string>);

      var idEntity = new Entity(DSUniqueSchemaStreamStateStorage.GetSchema());
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
        var settingIdEntity = dataStorage.GetEntity(DSUniqueSchemaStreamStateStorage.GetSchema());

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
        Entity settingEntity = dataStorage.GetEntity(StreamStateSchema.GetSchema());
        if (!settingEntity.IsValid())
          continue;

        return settingEntity;
      }
      return null;
    }
  }
}
