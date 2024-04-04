using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.ExtensibleStorage;
using Autodesk.Revit.UI;
using Revit.Async;
using Speckle.Connectors.DUI.Models;
using Speckle.Connectors.DUI.Models.Card;
using Speckle.Converters.RevitShared.Helpers;
using Speckle.Core.Logging;
using Speckle.Newtonsoft.Json;

namespace Speckle.Connectors.Revit.HostApp;

// POC: should be interfaced out
internal class RevitDocumentStore : DocumentModelStore
{
  // POC: move to somewhere central?
  private static readonly Guid s_revitDocumentStoreId = new("D35B3695-EDC9-4E15-B62A-D3FC2CB83FA3");

  private readonly RevitContext _revitContext;

  public RevitDocumentStore(RevitContext revitContext, JsonSerializerSettings serializerSettings)
    : base(serializerSettings)
  {
    _revitContext = revitContext;

    UIApplication uiApplication = _revitContext.UIApplication;

    uiApplication.ApplicationClosing += (_, _) => WriteToFile();

    uiApplication.Application.DocumentSaving += (_, _) => WriteToFile();
    uiApplication.Application.DocumentSynchronizingWithCentral += (_, _) => WriteToFile();

    uiApplication.ViewActivated += (_, e) =>
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

    uiApplication.Application.DocumentOpening += (_, _) => IsDocumentInit = false;
    uiApplication.Application.DocumentOpened += (_, _) => IsDocumentInit = false;
  }

  public override void WriteToFile()
  {
    UIDocument doc = _revitContext.UIApplication.ActiveUIDocument;

    // POC: this can happen?
    if (doc == null)
    {
      return;
    }

    RevitTask.RunAsync(() => {
      // POC: re-instate
      //using Transaction t = new(doc.Document, "Speckle Write State");
      //t.Start();
      //using DataStorage ds = GetSettingsDataStorage(doc.Document) ?? DataStorage.Create(doc.Document);

      //using Entity stateEntity = new(DocumentModelStoreSchema.GetSchema());
      //string serializedModels = Serialize();
      //stateEntity.Set("contents", serializedModels);

      //using Entity idEntity = new(IdStorageSchema.GetSchema());
      //idEntity.Set("Id", s_revitDocumentStoreId);

      //ds.SetEntity(idEntity);
      //ds.SetEntity(stateEntity);
      //t.Commit();
    });
  }

  public override void ReadFromFile()
  {
    try
    {
      Entity stateEntity = GetSpeckleEntity(_revitContext.UIApplication.ActiveUIDocument.Document);
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

  private DataStorage GetSettingsDataStorage(Document doc)
  {
    // POC: re-instate
    //using FilteredElementCollector collector = new(doc);
    //FilteredElementCollector dataStorages = collector.OfClass(typeof(DataStorage));

    //foreach (Element element in dataStorages)
    //{
    //  DataStorage dataStorage = (DataStorage)element;
    //  Entity settingIdEntity = dataStorage.GetEntity(IdStorageSchema.GetSchema());
    //  if (!settingIdEntity.IsValid())
    //  {
    //    continue;
    //  }

    //  Guid id = settingIdEntity.Get<Guid>("Id");
    //  if (!id.Equals(s_revitDocumentStoreId))
    //  {
    //    continue;
    //  }

    //  return dataStorage;
    //}
    return null;
  }

  private Entity GetSpeckleEntity(Document doc)
  {
    // POC: re-instate
    //using FilteredElementCollector collector = new(doc);

    //FilteredElementCollector dataStorages = collector.OfClass(typeof(DataStorage));
    //foreach (Element element in dataStorages)
    //{
    //  DataStorage dataStorage = (DataStorage)element;
    //  Entity settingEntity = dataStorage.GetEntity(DocumentModelStoreSchema.GetSchema());
    //  if (!settingEntity.IsValid())
    //  {
    //    continue;
    //  }

    //  return settingEntity;
    //}
    return null;
  }
}
