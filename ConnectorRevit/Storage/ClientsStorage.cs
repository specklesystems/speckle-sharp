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
  /// Manages the serialisation of speckle streams boxes
  /// (stream info, account info, and filter type) in a revit document.
  /// </summary>
  public static class StreamBoxStorageManager
  {
    readonly static Guid ID = new Guid("{5D453471-1F20-44CE-B1D0-BBD2BDE4616A}");

    /// <summary>
    /// Returns the speckle boxes present in the current document.
    /// </summary>
    /// <param name="doc"></param>
    /// <returns></returns>
    public static StreamBoxesWrapper ReadStreamBoxes(Document doc)
    {
      var streamBoxesEntity = GetSpeckleEntity(doc);
      if (streamBoxesEntity == null || !streamBoxesEntity.IsValid())
        return null;

      var myStreamBoxes = new StreamBoxesWrapper();
      myStreamBoxes.SetStreamBoxes(streamBoxesEntity.Get<IList<string>>("streamBoxes"));

      return myStreamBoxes;
    }

    /// <summary>
    /// Writes the stream box to the current document.
    /// </summary>
    /// <param name="doc"></param>
    /// <param name="wrap"></param>
    public static void WriteStreamBoxes(Document doc, StreamBoxesWrapper wrap)
    {
      var ds = GetSettingsDataStorage(doc);

      if (ds == null)
        ds = DataStorage.Create(doc);

      var streamBoxesEntity = new Entity(StreamBoxesSchema.GetSchema());

      streamBoxesEntity.Set("streamBoxes", wrap.GetStringList()as IList<string>);

      var idEntity = new Entity(DSUniqueSchemaStreamBoxStorage.GetSchema());
      idEntity.Set("Id", ID);

      ds.SetEntity(idEntity);
      ds.SetEntity(streamBoxesEntity);
    }

    private static DataStorage GetSettingsDataStorage(Document doc)
    {
      // Retrieve all data storages from project
      var collector = new FilteredElementCollector(doc);

      var dataStorages = collector.OfClass(typeof(DataStorage));

      // Find setting data storage
      foreach (DataStorage dataStorage in dataStorages)
      {
        var settingIdEntity = dataStorage.GetEntity(DSUniqueSchemaStreamBoxStorage.GetSchema());

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
        Entity settingEntity = dataStorage.GetEntity(StreamBoxesSchema.GetSchema());
        if (!settingEntity.IsValid())
          continue;

        return settingEntity;
      }
      return null;
    }
  }
}
