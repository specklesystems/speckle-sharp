using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Speckle.Connectors.DUI.Bindings;
using Speckle.Connectors.Utils.Cancellation;
using Speckle.Connectors.DUI.Bridge;
using Speckle.Connectors.Revit.HostApp;
using Speckle.Connectors.Revit.Plugin;
using Speckle.Core.Logging;
using Speckle.Connectors.Utils;
using Speckle.Autofac.DependencyInjection;
using Speckle.Converters.Common;

namespace Speckle.Connectors.Revit.Bindings;

internal class SendBinding : RevitBaseBinding, ICancelable, ISendBinding
{
  // POC:does it need injecting?
  public CancellationManager CancellationManager { get; } = new();

  // POC: does it need injecting?
  private HashSet<string> ChangedObjectIds { get; set; } = new();

  // In the context of the SEND operation, we're only ever expecting ONE conversion
  private readonly IScopedFactory<ISpeckleConverterToSpeckle> _speckleConverterToSpeckleFactory;
  private readonly ISpeckleConverterToSpeckle _speckleConverterToSpeckle;
  private readonly IRevitIdleManager _idleManager;

  public SendBinding(
    IScopedFactory<ISpeckleConverterToSpeckle> speckleConverterToSpeckleFactory,
    IRevitIdleManager idleManager,
    RevitContext revitContext,
    RevitDocumentStore store,
    IBridge bridge
  )
    : base("sendBinding", store, bridge, revitContext)
  {
    _speckleConverterToSpeckleFactory = speckleConverterToSpeckleFactory;
    _speckleConverterToSpeckle = _speckleConverterToSpeckleFactory.ResolveScopedInstance();
    _idleManager = idleManager;
    Commands = new SendBindingUICommands(bridge);
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

  public SendBindingUICommands Commands { get; }

  private void HandleSend(string modelCardId)
  {
    _speckleConverterToSpeckle.Convert();
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
    _idleManager.SubscribeToIdle(RunExpirationChecks);
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

    Commands.SetModelsExpired(expiredSenderIds);
    ChangedObjectIds = new HashSet<string>();
  }

  protected override void Disposing(bool isDipsosing, bool disposedState)
  {
    if (isDipsosing && !disposedState)
    {
      _speckleConverterToSpeckleFactory.Dispose();
    }
  }
}
