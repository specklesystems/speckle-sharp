using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.ExtensibleStorage;

namespace Speckle.ConnectorRevit.Storage
{
  /// <summary>
  /// Manages the serialisation of speckle clients in a revit document.
  /// </summary>
  public static class SpeckleClientsStorageManager
  {
    readonly static Guid ID = new Guid("{5D453471-1F20-44CE-B1D0-BBD2BDE4616A}");

    /// <summary>
    /// Returns the speckle clients present in the current document.
    /// </summary>
    /// <param name="doc"></param>
    /// <returns></returns>
    public static SpeckleClientsWrapper ReadClients(Document doc)
    {
      var speckleClientsEntity = GetSpeckleEntity(doc);
      if (speckleClientsEntity == null || !speckleClientsEntity.IsValid()) return null;

      var mySpeckleClients = new SpeckleClientsWrapper();
      mySpeckleClients.SetClients(speckleClientsEntity.Get<IList<string>>("clients"));

      return mySpeckleClients;
    }

    /// <summary>
    /// Writes the clients to the current document.
    /// </summary>
    /// <param name="doc"></param>
    /// <param name="wrap"></param>
    public static void WriteClients(Document doc, SpeckleClientsWrapper wrap)
    {
      var ds = GetSettingsDataStorage(doc);

      if (ds == null) ds = DataStorage.Create(doc);

      Entity speckleClientsEntity = new Entity(SpeckleClientsSchema.GetSchema());

      speckleClientsEntity.Set("clients", wrap.GetStringList() as IList<string>);

      Entity idEntity = new Entity(DSUniqueSchemaClientsStorage.GetSchema());
      idEntity.Set("Id", ID);

      ds.SetEntity(idEntity);
      ds.SetEntity(speckleClientsEntity);
    }

    private static DataStorage GetSettingsDataStorage(Document doc)
    {
      // Retrieve all data storages from project
      FilteredElementCollector collector = new FilteredElementCollector(doc);

      var dataStorages = collector.OfClass(typeof(DataStorage));

      // Find setting data storage
      foreach (DataStorage dataStorage in dataStorages)
      {
        Entity settingIdEntity = dataStorage.GetEntity(DSUniqueSchemaClientsStorage.GetSchema());

        if (!settingIdEntity.IsValid()) continue;

        var id = settingIdEntity.Get<Guid>("Id");

        if (!id.Equals(ID)) continue;

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
        Entity settingEntity = dataStorage.GetEntity(SpeckleClientsSchema.GetSchema());
        if (!settingEntity.IsValid()) continue;

        return settingEntity;
      }
      return null;
    }
  }
}
