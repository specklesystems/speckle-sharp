using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.ExtensibleStorage;
using Autodesk.Revit.UI;
using DUI3.Models;

namespace Speckle.ConnectorRevitDUI3.Utils;

public class RevitDocumentStore : DocumentModelStore
{
  private static UIApplication RevitApp { get; set; }
  private static UIDocument CurrentDoc => RevitApp.ActiveUIDocument;
  
  private static readonly Guid Guid = new Guid("D35B3695-EDC9-4E15-B62A-D3FC2CB83FA3");
  
  public RevitDocumentStore(UIApplication revitApp)
  {
    RevitApp = revitApp;
   
    RevitApp.ViewActivated += (_, e) =>
    {
      if (e.Document == null) return;
      if (e.PreviousActiveView?.Document.PathName == e.CurrentActiveView.Document.PathName) return;
      OnDocumentChanged();
    };

    RevitApp.Application.DocumentOpening += (_, _) => IsDocumentInit = false;
    RevitApp.Application.DocumentOpened += (_, _) => IsDocumentInit = true;
  }
  
  public override void WriteToFile()
  {
    using var ds = GetSettingsDataStorage(CurrentDoc.Document) ?? DataStorage.Create(CurrentDoc.Document);
    
    using var stateEntity = new Entity(DocumentModelStoreSchema.GetSchema());
    var serializedModels = Serialize();
    stateEntity.Set("contents", serializedModels);

    using var idEntity = new Entity(IdStorageSchema.GetSchema());
    idEntity.Set("Id", Guid);
    
    ds.SetEntity(idEntity);
    ds.SetEntity(stateEntity);
  }

  public override void ReadFromFile()
  {
    try
    {
      var stateEntity = GetSpeckleEntity(CurrentDoc.Document);
      if (stateEntity == null || !stateEntity.IsValid())
      {
        Models = new List<ModelCard>();
        return;
      }

      var modelsString = stateEntity.Get<string>("contents");
      Models = Deserialize(modelsString);
    }
    catch (Exception _)
    {
      Models = new List<ModelCard>();
    }
  }
  
  private static DataStorage GetSettingsDataStorage(Document doc)
  {
    using var collector = new FilteredElementCollector(doc);
    var dataStorages = collector.OfClass(typeof(DataStorage));

    foreach (var element in dataStorages)
    {
      var dataStorage = (DataStorage)element;
      var settingIdEntity = dataStorage.GetEntity(IdStorageSchema.GetSchema());
      if (!settingIdEntity.IsValid()) continue;

      var id = settingIdEntity.Get<Guid>("Id");
      if (!id.Equals(Guid)) continue;
      return dataStorage;
    }
    return null;
  }

  private static Entity GetSpeckleEntity(Document doc)
  {
    using var collector = new FilteredElementCollector(doc);

    var dataStorages = collector.OfClass(typeof(DataStorage));
    foreach(var element in dataStorages)
    {
      var dataStorage = (DataStorage)element;
      Entity settingEntity = dataStorage.GetEntity(DocumentModelStoreSchema.GetSchema());
      if (!settingEntity.IsValid())
        continue;

      return settingEntity;
    }
    return null;
  }
}
