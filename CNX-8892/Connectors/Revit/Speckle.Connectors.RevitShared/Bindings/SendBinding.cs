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
using Speckle.Core.Logging;
using Speckle.Connectors.Utils;
using Autofac.Features.Indexed;
using Speckle.Autofac.DependencyInjection;
using Speckle.Converters.Common;

namespace Speckle.Connectors.Revit.Bindings;

internal class SendBinding : RevitBaseBinding, ICancelable
{
  // POC:does it need injecting?
  public CancellationManager CancellationManager { get; } = new();

  // POC: does it need injecting?
  private HashSet<string> ChangedObjectIds { get; set; } = new();

  // POC: do we need all of these or can we look specifically for the revit one?
  // revit converter name should come from...  attribute?
  private readonly IIndex<string, IScopedFactory<IHostToSpeckleConverter>> _converters;

  public SendBinding(
    IIndex<string, IScopedFactory<IHostToSpeckleConverter>> converters,
    RevitContext revitContext,
    RevitDocumentStore store,
    IBridge bridge
  )
    : base("sendBinding", store, bridge, revitContext)
  {
    _converters = converters;

    // TODO expiry events
    // TODO filters need refresh events
    revitContext.UIApplication.Application.DocumentChanged += (_, e) => DocChangeHandler(e);
  }

  public List<ISendFilter> GetSendFilters()
  {
    return new List<ISendFilter> { new RevitEverythingFilter(), new RevitSelectionFilter() };
  }

  public async void Send(string modelCardId)
  {
    SpeckleTopLevelExceptionHandler.Run(
      () => HandleSend(modelCardId),
      HandleSpeckleException,
      HandleUnexpectedException,
      HandleFatalException
    );
  }

  public void CancelSend(string modelCardId)
  {
    CancellationManager.CancelOperation(modelCardId);
  }

  private void HandleSend(string modelCardId)
  {
    int t = -1;
  }

  private bool HandleSpeckleException(SpeckleException spex)
  {
    // POC: do something here

    return false;
  }

  private bool HandleUnexpectedException(Exception ex)
  {
    // POC: do something here

    return false;
  }

  private bool HandleFatalException(Exception ex)
  {
    // POC: do something here

    return false;
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

    SendBindingUICommands.SetModelsExpired(Parent, expiredSenderIds);
    ChangedObjectIds = new HashSet<string>();
  }
}
