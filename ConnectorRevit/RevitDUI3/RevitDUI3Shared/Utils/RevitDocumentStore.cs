using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.ExtensibleStorage;
using Autodesk.Revit.UI;
using DUI3.Models;
using Revit.Async;
using Speckle.Core.Logging;

namespace Speckle.ConnectorRevitDUI3.Utils;

public class RevitDocumentStore : DocumentModelStore
{
  private static UIApplication RevitApp { get; set; }
  private static UIDocument CurrentDoc => RevitApp.ActiveUIDocument;

  private static readonly Guid s_guid = new("D35B3695-EDC9-4E15-B62A-D3FC2CB83FA3");

  public RevitDocumentStore()
  {
    RevitApp = RevitAppProvider.RevitApp;

    RevitApp.ApplicationClosing += (_, _) => WriteToFile();
    RevitApp.Application.DocumentSaving += (_, _) => WriteToFile();
    RevitApp.Application.DocumentSynchronizingWithCentral += (_, _) => WriteToFile();

    RevitApp.ViewActivated += (_, e) =>
    {
      if (e.Document == null)
      {
        return;
      }

      if (e.PreviousActiveView?.Document.PathName == e.CurrentActiveView.Document.PathName)
      {
        return;
      }

      IsDocumentInit = true;
      ReadFromFile();
      OnDocumentChanged();
    };

    RevitApp.Application.DocumentOpening += (_, _) => IsDocumentInit = false;
    RevitApp.Application.DocumentOpened += (_, _) => IsDocumentInit = false;
  }

  public override void WriteToFile()
  {
    if (CurrentDoc == null)
    {
      return;
    }

    RevitTask.RunAsync(() =>
    {
      using Transaction t = new(CurrentDoc.Document, "Speckle Write State");
      t.Start();
      using DataStorage ds = GetSettingsDataStorage(CurrentDoc.Document) ?? DataStorage.Create(CurrentDoc.Document);

      using Entity stateEntity = new(DocumentModelStoreSchema.GetSchema());
      string serializedModels = Serialize();
      stateEntity.Set("contents", serializedModels);

      using Entity idEntity = new(IdStorageSchema.GetSchema());
      idEntity.Set("Id", s_guid);

      ds.SetEntity(idEntity);
      ds.SetEntity(stateEntity);
      t.Commit();
    });
  }

  public override void ReadFromFile()
  {
    try
    {
      Entity stateEntity = GetSpeckleEntity(CurrentDoc.Document);
      if (stateEntity == null || !stateEntity.IsValid())
      {
        Models = new List<ModelCard>();
        return;
      }

      string modelsString = stateEntity.Get<string>("contents");
      Models = Deserialize(modelsString);
    }
    catch (SpeckleException)
    {
      Models = new List<ModelCard>();
    }
  }

  private static DataStorage GetSettingsDataStorage(Document doc)
  {
    using FilteredElementCollector collector = new(doc);
    FilteredElementCollector dataStorages = collector.OfClass(typeof(DataStorage));

    foreach (Element element in dataStorages)
    {
      DataStorage dataStorage = (DataStorage)element;
      Entity settingIdEntity = dataStorage.GetEntity(IdStorageSchema.GetSchema());
      if (!settingIdEntity.IsValid())
      {
        continue;
      }

      Guid id = settingIdEntity.Get<Guid>("Id");
      if (!id.Equals(s_guid))
      {
        continue;
      }

      return dataStorage;
    }
    return null;
  }

  private static Entity GetSpeckleEntity(Document doc)
  {
    using FilteredElementCollector collector = new(doc);

    FilteredElementCollector dataStorages = collector.OfClass(typeof(DataStorage));
    foreach (Element element in dataStorages)
    {
      DataStorage dataStorage = (DataStorage)element;
      Entity settingEntity = dataStorage.GetEntity(DocumentModelStoreSchema.GetSchema());
      if (!settingEntity.IsValid())
      {
        continue;
      }

      return settingEntity;
    }
    return null;
  }
}
