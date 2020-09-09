using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Speckle.DesktopUI.Utils;

namespace Speckle.DesktopUI
{
  public abstract class ConnectorBindings
  {
    public ConnectorBindings()
    {

    }

    /// <summary>
    /// Sends an event to the UI, bound to the global EventBus.
    /// </summary>
    /// <param name="eventName">The event's name.</param>
    /// <param name="eventInfo">The event args, which will be serialised to a string.</param>
    public virtual void NotifyUi(string eventName, dynamic eventInfo)
    {

    }

    public virtual string GetFilters()
    {
      return JsonConvert.SerializeObject(GetSelectionFilters());
    }

    public virtual void StartProcess(string args)
    {
      try
      {
        Process.Start(args);
      }
      catch (Exception e)
      {

      }

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


    /// <summary>
    /// Returns the serialised clients present in the current open host file.
    /// </summary>
    /// <returns></returns>
    public abstract string GetFileClients();

    /// <summary>
    /// Adds a sender and persits the info to the host file
    /// </summary>
    public abstract void AddSender(string args);

    /// <summary>
    /// Updates a sender and persits the info to the host file
    /// </summary>
    public abstract void UpdateSender(string args);

    /// <summary>
    /// Pushes a sender's stream
    /// </summary>
    public abstract void PushSender(string args);

    /// <summary>
    /// Adds the current selection to the provided client.
    /// </summary>
    public abstract void AddSelectionToSender(string args);

    /// <summary>
    /// Removes the current selection from the provided client.
    /// </summary>
    /// <param name="args"></param>
    public abstract void RemoveSelectionFromSender(string args);

    /// <summary>
    /// Adds a receiver and persits the info to the host file
    /// </summary>
    public abstract void AddReceiver(string args);

    /// <summary>
    /// Removes a client from the file and persists the info to the host file.
    /// </summary>
    /// <param name="args"></param>
    public abstract void RemoveClient(string args);

    /// <summary>
    /// Bakes the specified client in the host file.
    /// </summary>
    /// <param name="args"></param>
    public abstract void BakeReceiver(string args);

    // TODO: See how we go about this
    public abstract void AddObjectsToSender(string args);
    public abstract void RemoveObjectsFromSender(string args);

    /// <summary>
    /// clients should be able to select/preview/hover one way or another their associated objects
    /// </summary>
    /// <param name="args"></param>
    public abstract void SelectClientObjects(string args);

    public abstract List<ISelectionFilter> GetSelectionFilters();

    #endregion
  }
}
