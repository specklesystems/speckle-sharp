using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using Speckle.Core.Kits;
using Speckle.Core.Credentials;
using Speckle.Core.Transports;
using Speckle.Core.Models;
using Speckle.Connectors.DUI.Bindings;
using Speckle.Connectors.Utils.Cancellation;
using Speckle.Connectors.DUI.Bridge;
using Speckle.Connectors.Revit.HostApp;
using Speckle.Connectors.Revit.Plugin;
using Speckle.Connectors.Revit.Bindings;

namespace Speckle.Connectors.Revit.Bindings;

internal class SendBinding : RevitBaseBinding, ICancelable
{
  // POC: needs injection
  public CancellationManager CancellationManager { get; } = new();

  private HashSet<string> ChangedObjectIds { get; set; } = new();

  public SendBinding(RevitContext revitContext, RevitDocumentStore store, IBridge bridge, IBrowserSender browserSender)
    : base("sendBinding", store, bridge, browserSender, revitContext)
  {
    // TODO expiry events
    // TODO filters need refresh events
    revitContext.UIApplication.Application.DocumentChanged += (_, e) => DocChangeHandler(e);
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

    //var convertedObjects = new List<Base>();
    //int count = 0;
    //foreach (var revitElement in elements)
    //{
    //  if (cts.IsCancellationRequested)
    //  {
    //    Progress.CancelSend(Parent, modelCardId, (double)count / elements.Count);
    //    // throw new OperationCanceledException(); TBD -> Not sure
    //    break;
    //  }

    //  count++;
    //  convertedObjects.Add(converter.ConvertToSpeckle(revitElement));
    //  double progress = (double)count / elements.Count;
    //  Progress.SenderProgressToBrowser(Parent, modelCardId, progress);
    //}

    //commitObject["@elements"] = convertedObjects;

    return commitObject;
  }

  public async void Send(string modelCardId)
  {
    try
    {
      //// 0 - Init cancellation token source -> Manager also cancel it if exist before
      //CancellationTokenSource cts = CancellationManager.InitCancellationTokenSource(modelCardId);

      //// 1 - Get model
      //SenderModelCard model = _store.GetModelById(modelCardId) as SenderModelCard;

      //// 2 - Check account exist
      //Account account = Accounts.GetAccount(model.AccountId);

      //// 3 - Get elements to convert
      //List<Element> elements = Utils.Elements.GetElementsFromDocument(Doc, model.SendFilter.GetObjectIds());

      //// 4 - Get converter
      //ISpeckleConverter converter = Converters.GetConverter(Doc, RevitAppProvider.Version());

      //// 5 - Convert objects
      //Base commitObject = ConvertElements(elements, converter, modelCardId, cts);

      //if (cts.IsCancellationRequested)
      //{
      //  return;
      //}

      //// 6 - Get transports
      //List<ITransport> transports = new() { new ServerTransport(account, model.ProjectId) };

      //// 7 - Serialize and Send objects
      //string objectId = await Operations
      //  .Send(Parent, modelCardId, commitObject, transports, cts.Token)
      //  .ConfigureAwait(true);

      //if (cts.IsCancellationRequested)
      //{
      //  return;
      //}

      //// 8 - Create Version
      //Operations.CreateVersion(Parent, model, objectId, "Revit");
    }
    catch (Exception e)
    {
      //if (e is OperationCanceledException)
      //{
      //  Progress.CancelSend(Parent, modelCardId);
      //  return;
      //}
      //// TODO: Init here class to handle send errors to report UI, Seq etc..
      //throw;
    }
  }

  public void CancelSend(string modelCardId)
  {
    CancellationManager.CancelOperation(modelCardId);
  }

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
    //    RevitIdleManager.SubscribeToIdle(RunExpirationChecks);
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

    _browserSender.Send(Parent.FrontendBoundName, SendBindingEvents.SendersExpired, expiredSenderIds);
    ChangedObjectIds = new HashSet<string>();
  }
}
