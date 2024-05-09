using System.Diagnostics;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.ExtensibleStorage;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Events;
using Speckle.Connectors.DUI.Models;
using Speckle.Connectors.DUI.Models.Card;
using Speckle.Connectors.Utils;
using Speckle.Converters.RevitShared.Helpers;
using Speckle.Core.Logging;
using Speckle.Newtonsoft.Json;

namespace Speckle.Connectors.Revit.HostApp;

// POC: should be interfaced out
internal sealed class RevitDocumentStore : DocumentModelStore
{
  // POC: move to somewhere central?
  private static readonly Guid s_revitDocumentStoreId = new("D35B3695-EDC9-4E15-B62A-D3FC2CB83FA3");

  private readonly RevitContext _revitContext;
  private readonly DocumentModelStorageSchema _documentModelStorageSchema;
  private readonly IdStorageSchema _idStorageSchema;

  public RevitDocumentStore(
    RevitContext revitContext,
    JsonSerializerSettings serializerSettings,
    DocumentModelStorageSchema documentModelStorageSchema,
    IdStorageSchema idStorageSchema
  )
    : base(serializerSettings)
  {
    _revitContext = revitContext;
    _documentModelStorageSchema = documentModelStorageSchema;
    _idStorageSchema = idStorageSchema;

    UIApplication uiApplication = _revitContext.UIApplication.NotNull();
    uiApplication.ApplicationClosing += (_, _) => WriteToFile(); // POC: Not sure why we would need it since we have save and clos events
    uiApplication.Application.DocumentSaving += (_, _) => WriteToFile();
    uiApplication.Application.DocumentSavingAs += (_, _) => WriteToFile();
    uiApplication.Application.DocumentSynchronizingWithCentral += (_, _) => WriteToFile(); // POC: Not sure why we have it

    uiApplication.ViewActivated += OnViewActivated;

    uiApplication.Application.DocumentOpening += (_, _) => IsDocumentInit = false;
    uiApplication.Application.DocumentOpened += (_, _) => IsDocumentInit = false;
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
    if (e.PreviousActiveView?.Document != null && e.PreviousActiveView.Document.Equals(e.CurrentActiveView.Document))
    {
      return;
    }

    if (e.PreviousActiveView?.Document != null)
    {
      WriteToFileWithDoc(e.PreviousActiveView.Document);
    }

    IsDocumentInit = true;
    ReadFromFile();
    OnDocumentChanged();
  }

  private void WriteToFileWithDoc(Document doc)
  {
    // POC: this can happen?
    if (doc == null)
    {
      return;
    }

    string serializedModels = Serialize();

    using Transaction t = new(doc, "Speckle Write State");
    t.Start();
    using DataStorage ds = GetSettingsDataStorage(doc) ?? DataStorage.Create(doc);

    using Entity stateEntity = new(_documentModelStorageSchema.GetSchema());
    // string serializedModels = Serialize();
    stateEntity.Set("contents", serializedModels);

    using Entity idEntity = new(_idStorageSchema.GetSchema());
    idEntity.Set("Id", s_revitDocumentStoreId);

    ds.SetEntity(idEntity);
    ds.SetEntity(stateEntity);
    t.Commit();
  }

  public override void WriteToFile() =>
    WriteToFileWithDoc(_revitContext.UIApplication.NotNull().ActiveUIDocument.Document);

  public override void ReadFromFile()
  {
    try
    {
      var stateEntity = GetSpeckleEntity(_revitContext.UIApplication?.ActiveUIDocument.Document);
      if (stateEntity == null || !stateEntity.IsValid())
      {
        Models = new List<ModelCard>();
        return;
      }

      string modelsString = stateEntity.Get<string>("contents");
      Models = Deserialize(modelsString).NotNull();
    }
    catch (Exception ex) when (!ex.IsFatal())
    {
      Models = new List<ModelCard>();
      Debug.WriteLine(ex.Message); // POC: Log here error and notify UI that cards not read succesfully
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

  private Entity? GetSpeckleEntity(Document? doc)
  {
    if (doc is null)
    {
      return null;
    }
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
