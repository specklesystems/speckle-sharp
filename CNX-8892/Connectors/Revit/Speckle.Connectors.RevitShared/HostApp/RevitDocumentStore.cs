using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.ExtensibleStorage;
using Autodesk.Revit.UI;
using Revit.Async;
using Speckle.Connectors.DUI.Models;
using Speckle.Connectors.DUI.Models.Card;
using Speckle.Connectors.Revit.Plugin;
using Speckle.Core.Logging;
using Speckle.Newtonsoft.Json;

namespace Speckle.Connectors.Revit.HostApp;

internal class RevitDocumentStore : DocumentModelStore
{
  private static readonly Guid s_revitDocumentStoreId = new("D35B3695-EDC9-4E15-B62A-D3FC2CB83FA3");

  private readonly UIApplication _uiApplication;

  public RevitDocumentStore(IRevitPlugin revitPlugin, JsonSerializerSettings serializerOption)
    : base(serializerOption)
  {
    _uiApplication = revitPlugin.UiApplication;

    _uiApplication.ApplicationClosing += (_, _) => WriteToFile();

    _uiApplication.Application.DocumentSaving += (_, _) => WriteToFile();
    _uiApplication.Application.DocumentSynchronizingWithCentral += (_, _) => WriteToFile();

    _uiApplication.ViewActivated += (_, e) =>
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

    _uiApplication.Application.DocumentOpening += (_, _) => IsDocumentInit = false;
    _uiApplication.Application.DocumentOpened += (_, _) => IsDocumentInit = false;
  }

  public override void WriteToFile()
  {
    UIDocument doc = _uiApplication.ActiveUIDocument;

    // POC: this can happen?
    if (doc == null)
    {
      return;
    }

    RevitTask.RunAsync(() =>
    {
      using Transaction t = new(doc.Document, "Speckle Write State");
      t.Start();
      using DataStorage ds = GetSettingsDataStorage(doc.Document) ?? DataStorage.Create(doc.Document);

      using Entity stateEntity = new(DocumentModelStoreSchema.GetSchema());
      string serializedModels = Serialize();
      stateEntity.Set("contents", serializedModels);

      using Entity idEntity = new(IdStorageSchema.GetSchema());
      idEntity.Set("Id", s_revitDocumentStoreId);

      ds.SetEntity(idEntity);
      ds.SetEntity(stateEntity);
      t.Commit();
    });
  }

  public override void ReadFromFile()
  {
    try
    {
      Entity stateEntity = GetSpeckleEntity(_uiApplication.ActiveUIDocument.Document);
      if (stateEntity == null || !stateEntity.IsValid())
      {
        Models = new List<ModelCard>();
        return;
      }

      string modelsString = stateEntity.Get<string>("contents");
      Models = Deserialize(modelsString);
    }
    // POC: hmmmmm, is this valid? Do we really throw an exception if the entity does not exist?
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
      if (!id.Equals(s_revitDocumentStoreId))
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
