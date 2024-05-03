using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.ExtensibleStorage;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Events;
using Revit.Async;
using Speckle.Connectors.DUI.Models;
using Speckle.Connectors.DUI.Models.Card;
using Speckle.Connectors.Revit.Plugin;
using Speckle.Connectors.Utils.Operations;
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
  private readonly DocumentModelStorageSchema _documentModelStorageSchema;
  private readonly IdStorageSchema _idStorageSchema;
  private readonly IRevitIdleManager _idleManager;

  public RevitDocumentStore(
    RevitContext revitContext,
    JsonSerializerSettings serializerSettings,
    DocumentModelStorageSchema documentModelStorageSchema,
    IRevitIdleManager idleManager,
    IdStorageSchema idStorageSchema
  )
    : base(serializerSettings)
  {
    _revitContext = revitContext;
    _documentModelStorageSchema = documentModelStorageSchema;
    _idStorageSchema = idStorageSchema;
    _idleManager = idleManager;

    UIApplication uiApplication = _revitContext.UIApplication;
    // uiApplication.ApplicationClosing += (_, _) => WriteToFile(); // POC: Not sure why we would need it since we have save and clos events
    uiApplication.Application.DocumentSaving += (_, _) => WriteToFile();
    uiApplication.Application.DocumentSavingAs += (_, _) => WriteToFile();
    // uiApplication.Application.DocumentClosing += (_, _) => WriteToFile();
    // uiApplication.Application.DocumentSynchronizingWithCentral += (_, _) => WriteToFile(); // POC: Not sure why we have it

    uiApplication.ViewActivating += OnViewActivating;
    uiApplication.ViewActivated += OnViewActivated;

    uiApplication.Application.DocumentOpening += (_, _) => IsDocumentInit = false;
    uiApplication.Application.DocumentOpened += (_, _) => RunDocInitOperations();
  }

  private void RunDocInitOperations()
  {
    IsDocumentInit = true;
    ReadFromFile();
    OnDocumentChanged();
  }

  /// <summary>
  /// This is the place where we track document switch for old document -> Responsible to Write into old
  /// </summary>
  private void OnViewActivating(object sender, ViewActivatingEventArgs e)
  {
    if (e.Document == null)
    {
      return;
    }

    if (e.NewActiveView.Document.Equals(e.CurrentActiveView.Document))
    {
      return;
    }

    WriteToFile();
  }

  /// <summary>
  /// This is the place where we track document switch for new document -> Responsible to Read from new doc
  /// </summary>
  private void OnViewActivated(object sender, ViewActivatedEventArgs e)
  {
    if (e.Document == null)
    {
      return;
    }

    // Return only if we are switching views that belongs to same document
    if (e.PreviousActiveView is not null && e.PreviousActiveView.Document.Equals(e.CurrentActiveView.Document))
    {
      return;
    }

    RunDocInitOperations();
  }

  public override void WriteToFile()
  {
    UIDocument doc = _revitContext.UIApplication.ActiveUIDocument;

    // POC: this can happen?
    if (doc == null)
    {
      return;
    }

    // NOTE: Document switched fixed by putting seralization outside of the RevitTask, otherwise it tries to serialize
    // empty document state since we create new one per document.
    string serializedModels = Serialize();

    // POC: previously we were calling below code
    RevitTask
      .RunAsync(() =>
      {
        using Transaction t = new(doc.Document, "Speckle Write State");
        t.Start();
        using DataStorage ds = GetSettingsDataStorage(doc.Document) ?? DataStorage.Create(doc.Document);

        using Entity stateEntity = new(_documentModelStorageSchema.GetSchema());
        // string serializedModels = Serialize();
        stateEntity.Set("contents", serializedModels);

        using Entity idEntity = new(_idStorageSchema.GetSchema());
        idEntity.Set("Id", s_revitDocumentStoreId);

        ds.SetEntity(idEntity);
        ds.SetEntity(stateEntity);
        t.Commit();
      })
      .ConfigureAwait(false);
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
    catch (Exception ex) when (!ex.IsFatal())
    {
      Debug.WriteLine(ex.Message); // POC: !!??
    }
  }

  private DataStorage? GetSettingsDataStorage(Document doc)
  {
    using FilteredElementCollector collector = new(doc);
    FilteredElementCollector dataStorages = collector.OfClass(typeof(DataStorage));

    foreach (Element element in dataStorages)
    {
      DataStorage dataStorage = (DataStorage)element;
      Entity settingIdEntity = dataStorage.GetEntity(_idStorageSchema.GetSchema());
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

  private Entity? GetSpeckleEntity(Document doc)
  {
    using FilteredElementCollector collector = new(doc);

    FilteredElementCollector dataStorages = collector.OfClass(typeof(DataStorage));
    foreach (Element element in dataStorages)
    {
      DataStorage dataStorage = (DataStorage)element;
      Entity settingEntity = dataStorage.GetEntity(_documentModelStorageSchema.GetSchema());
      if (!settingEntity.IsValid())
      {
        continue;
      }

      return settingEntity;
    }

    return null;
  }
}
