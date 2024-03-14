using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using DUI3;
using DUI3.Bindings;
using DUI3.Models.Card;
using DUI3.Operations;
using Speckle.ConnectorRevitDUI3.Utils;
using Speckle.Core.Kits;
using Speckle.Core.Credentials;
using Speckle.Core.Transports;
using Speckle.Core.Models;
using DUI3.Utils;
using Speckle.Core.Api;
using Operations = DUI3.Operations.Operations;

namespace Speckle.ConnectorRevitDUI3.Bindings;

public class SendBinding : ISendBinding, ICancelable
{
  public string Name { get; set; } = "sendBinding";
  public IBridge Parent { get; set; }

  private readonly RevitDocumentStore _store;

  private static UIApplication s_revitApp;

  private static Document Doc => s_revitApp.ActiveUIDocument.Document;

  public CancellationManager CancellationManager { get; } = new();

  private HashSet<string> ChangedObjectIds { get; set; } = new();

  public SendBinding(RevitDocumentStore store)
  {
    s_revitApp = RevitAppProvider.RevitApp;
    _store = store;

    // TODO expiry events
    // TODO filters need refresh events
    s_revitApp.Application.DocumentChanged += (_, e) => DocChangeHandler(e);
  }

  public List<ISendFilter> GetSendFilters()
  {
    return new List<ISendFilter> { new RevitEverythingFilter(), new RevitSelectionFilter() };
  }

  private Base ConvertElements(
    List<Element> elements,
    ISpeckleConverter converter,
    string modelCardId,
    CancellationTokenSource cts
  )
  {
    var commitObject = new Base();

    var convertedObjects = new List<Base>();
    int count = 0;
    foreach (var revitElement in elements)
    {
      if (cts.IsCancellationRequested)
      {
        throw new OperationCanceledException();
      }

      count++;
      convertedObjects.Add(converter.ConvertToSpeckle(revitElement));
      double progress = (double)count / elements.Count;
      BasicConnectorBindingCommands.SetModelProgress(
        Parent,
        modelCardId,
        new ModelCardProgress() { Status = "Converting", Progress = progress }
      );
    }

    commitObject["@elements"] = convertedObjects;

    return commitObject;
  }

  public async void Send(string modelCardId)
  {
    try
    {
      // 0 - Init cancellation token source -> Manager also cancel it if exist before
      CancellationTokenSource cts = CancellationManager.InitCancellationTokenSource(modelCardId);

      // 1 - Get model
      SenderModelCard model = _store.GetModelById(modelCardId) as SenderModelCard;

      // 2 - Check account exist
      Account account = Accounts.GetAccount(model.AccountId);

      // 3 - Get elements to convert
      List<Element> elements = Utils.Elements.GetElementsFromDocument(Doc, model.SendFilter.GetObjectIds());

      // 4 - Get converter
      ISpeckleConverter converter = Converters.GetConverter(Doc, RevitAppProvider.Version());

      // 5 - Convert objects
      Base commitObject = ConvertElements(elements, converter, modelCardId, cts);

      if (cts.IsCancellationRequested)
      {
        throw new OperationCanceledException(cts.Token);
      }

      // 6 - Get transports
      List<ITransport> transports = new() { new ServerTransport(account, model.ProjectId) };

      // 7 - Serialize and Send objects
      BasicConnectorBindingCommands.SetModelProgress(
        Parent,
        modelCardId,
        new ModelCardProgress { Status = "Uploading..." }
      );
      string objectId = await Speckle.Core.Api.Operations
        .Send(commitObject, cts.Token, transports, disposeTransports: true)
        .ConfigureAwait(true);

      BasicConnectorBindingCommands.SetModelProgress(
        Parent,
        modelCardId,
        new ModelCardProgress { Status = "Linking version to model..." }
      );

      // 8 - Create Version
      var apiClient = new Client(account);
      string versionId = await apiClient
        .CommitCreate(
          new CommitCreateInput()
          {
            streamId = model.ProjectId,
            branchName = model.ModelId,
            sourceApplication = "Rhino",
            objectId = objectId
          },
          cts.Token
        )
        .ConfigureAwait(true);

      SendBindingUiCommands.SetModelCreatedVersionId(Parent, modelCardId, versionId);
      apiClient.Dispose();
    }
    catch (Exception e)
    {
      if (e is OperationCanceledException)
      {
        return;
      }

      BasicConnectorBindingCommands.SetModelError(Parent, modelCardId, e);
    }
  }

  public void CancelSend(string modelCardId) => CancellationManager.CancelOperation(modelCardId);

  /// <summary>
  /// Keeps track of the changed element ids as well as checks if any of them need to trigger
  /// a filter refresh (e.g., views being added).
  /// </summary>
  /// <param name="e"></param>
  private void DocChangeHandler(Autodesk.Revit.DB.Events.DocumentChangedEventArgs e)
  {
    ICollection<ElementId> addedElementIds = e.GetAddedElementIds();
    ICollection<ElementId> deletedElementIds = e.GetDeletedElementIds();
    ICollection<ElementId> modifiedElementIds = e.GetModifiedElementIds();

    foreach (ElementId elementId in addedElementIds)
    {
      ChangedObjectIds.Add(elementId.IntegerValue.ToString());
    }

    foreach (ElementId elementId in deletedElementIds)
    {
      ChangedObjectIds.Add(elementId.IntegerValue.ToString());
    }

    foreach (ElementId elementId in modifiedElementIds)
    {
      ChangedObjectIds.Add(elementId.IntegerValue.ToString());
    }

    // TODO: CHECK IF ANY OF THE ABOVE ELEMENTS NEED TO TRIGGER A FILTER REFRESH
    // POC: re-instate
    // RevitIdleManager.SubscribeToIdle(RunExpirationChecks);
  }

  private void RunExpirationChecks()
  {
    List<SenderModelCard> senders = _store.GetSenders();
    List<string> expiredSenderIds = new();

    foreach (var sender in senders)
    {
      bool isExpired = sender.SendFilter.CheckExpiry(ChangedObjectIds.ToArray());
      if (isExpired)
      {
        expiredSenderIds.Add(sender.ModelCardId);
      }
    }
    SendBindingUiCommands.SetModelsExpired(Parent, expiredSenderIds);
    ChangedObjectIds = new HashSet<string>();
  }
}
