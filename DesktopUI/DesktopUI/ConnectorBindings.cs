using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Speckle.DesktopUI.Utils;
using Stylet;
using StyletIoC;

namespace Speckle.DesktopUI
{
  public abstract class ConnectorBindings
  {
    [Inject]
    private IEventAggregator _events;

    public ConnectorBindings() { }

    /// <summary>
    /// Sends an event to the UI. The event types are pre-defined and inherit from EventBase.
    /// </summary>
    /// <param name="notifyEvent">The event to be published</param>
    public virtual void NotifyUi(EventBase notifyEvent)
    {
      _events.PublishOnUIThread(notifyEvent);
    }

    /// <summary>
    /// Raise a toast notification which is shown in the bottom of the main UI window.
    /// </summary>
    /// <param name="message">The body of the notification</param>
    public virtual void RaiseNotification(string message)
    {
      var notif = new ShowNotificationEvent() { Notification = message };
      NotifyUi(notif);
    }

    public virtual string GetFilters()
    {
      return JsonConvert.SerializeObject(GetSelectionFilters());
    }

    public virtual bool CanSelectObjects()
    {
      return false;
    }

    public virtual bool CanTogglePreview()
    {
      return false;
    }

    #region abstract methods

    public abstract string GetApplicationHostName();
    public abstract string GetFileName();
    public abstract string GetDocumentId();
    public abstract string GetDocumentLocation();
    public abstract string GetActiveViewName();

    /// <summary>
    /// Returns the serialised clients present in the current open host file.
    /// </summary>
    /// <returns></returns>
    public abstract List<StreamState> GetFileContext();

    /// <summary>
    /// Adds a new client and persists the info to the host file
    /// </summary>
    public abstract void AddNewStream(StreamState state);

    /// <summary>
    /// Updates a client and persists the info to the host file
    /// </summary>
    public abstract void UpdateStream(StreamState state);

    /// <summary>
    /// Pushes a client's stream
    /// </summary>
    /// <param name="state"></param>
    /// <param name="progress"></param>
    public abstract Task<StreamState> SendStream(StreamState state);

    /// <summary>
    /// Receives stream data from the server
    /// </summary>
    /// <param name="state"></param>
    /// <param name="progress"></param>
    /// <returns></returns>
    public abstract Task<StreamState> ReceiveStream(StreamState state);

    /// <summary>
    /// Adds the current selection to the provided client.
    /// </summary>
    public abstract List<string> GetSelectedObjects();

    /// <summary>
    /// Gets a list of objects in the currently active view
    /// </summary>
    /// <returns></returns>
    public abstract List<string> GetObjectsInView();

    /// <summary>
    /// Adds a receiver and persists the info to the host file
    /// </summary>
    public abstract void AddExistingStream(string args);

    /// <summary>
    /// Removes a client from the file and persists the info to the host file.
    /// </summary>
    /// <param name="args"></param>
    public abstract void RemoveStream(string args);

    /// <summary>
    /// Bakes the specified client in the host file.
    /// </summary>
    /// <param name="args"></param>
    public abstract void BakeStream(string args);

    /// <summary>
    /// Removes the current selection from the provided client.
    /// </summary>
    /// <param name="args"></param>
    public abstract void RemoveSelectionFromClient(string args);

    // TODO: See how we go about this
    public abstract void AddObjectsToClient(string args);
    public abstract void RemoveObjectsFromClient(string args);

    /// <summary>
    /// clients should be able to select/preview/hover one way or another their associated objects
    /// </summary>
    /// <param name="args"></param>
    public abstract void SelectClientObjects(string args);

    public abstract List<ISelectionFilter> GetSelectionFilters();

    #endregion

    // ! to remove
    // just to play by triggering notifications in the main UI window
    public void TestBindings(string message)
    {
      var notif = new ShowNotificationEvent()
      {
        Notification = message
      };
      NotifyUi(notif);
    }
  }
}
