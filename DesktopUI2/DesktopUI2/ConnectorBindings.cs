using DesktopUI2.Models;
using DesktopUI2.Models.Filters;
using DesktopUI2.Models.Settings;
using DesktopUI2.ViewModels;
using Sentry.Reflection;
using Speckle.Core.Kits;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DesktopUI2
{
  public delegate void UpdateSavedStreams(List<StreamState> streams);
  public delegate void UpdateSelectedStream();


  public abstract class ConnectorBindings
  {
    public ConnectorBindings() { }

    public string ConnectorVersion =>
      System.Reflection.Assembly.GetAssembly(GetType()).GetNameAndVersion().Version;

    public string ConnectorName =>
      System.Reflection.Assembly.GetAssembly(GetType()).GetNameAndVersion().Name;

    //public List<StreamState> SavedStreamStates = new List<StreamState>();

    #region delegates

    public UpdateSavedStreams UpdateSavedStreams;
    public UpdateSelectedStream UpdateSelectedStream;

    #endregion

    public virtual bool CanSelectObjects()
    {
      return false;
    }

    public virtual bool CanTogglePreview()
    {
      return false;
    }

    #region abstract methods

    /// <summary>
    /// Gets the current host application name with version.
    /// </summary>
    /// <returns></returns>
    public abstract string GetHostAppNameVersion();

    /// <summary>
    /// Gets the current host application name.
    /// </summary>
    /// <returns></returns>
    public abstract string GetHostAppName();

    /// <summary>
    /// Gets the current opened/focused file's name.
    /// Make sure to check regarding unsaved/temporary files.
    /// </summary>
    /// <returns></returns>
    public abstract string GetFileName();

    /// <summary>
    /// Gets the current opened/focused file's id. 
    /// Generate one in here if the host app does not provide one.
    /// </summary>
    /// <returns></returns>
    public abstract string GetDocumentId();

    /// <summary>
    /// Gets the current opened/focused file's locations.
    /// Make sure to check regarding unsaved/temporary files.
    /// </summary>
    /// <returns></returns>
    public abstract string GetDocumentLocation();

    /// <summary>
    /// Gets the current opened/focused file's view, if applicable.
    /// </summary>
    /// <returns></returns>
    public abstract string GetActiveViewName();

    /// <summary>
    /// Returns the serialised clients present in the current open host file.
    /// </summary>
    /// <returns></returns>
    public abstract List<StreamState> GetStreamsInFile();


    /// <summary>
    /// Writes serialised clients to the current open host file.
    /// </summary>
    /// <returns></returns>
    public abstract void WriteStreamsToFile(List<StreamState> streams);

    /// <summary>
    /// Adds a new client and persists the info to the host file
    /// </summary>
    //public abstract void AddNewStream(StreamState state);

    /// <summary>
    /// Persists the stream info to the host file; if maintaining a local in memory copy, make sure to update it too.
    /// </summary>
    //public abstract void PersistAndUpdateStreamInFile(StreamState state);

    /// <summary>
    /// Pushes a client's stream
    /// </summary>
    /// <param name="state"></param>
    public abstract Task<string> SendStream(StreamState state, ProgressViewModel progress);

    /// <summary>
    /// Receives stream data from the server
    /// </summary>
    /// <param name="state"></param>
    /// <returns></returns>
    public abstract Task<StreamState> ReceiveStream(StreamState state, ProgressViewModel progress);

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
    /// Removes a client from the file and updates the host file.
    /// </summary>
    /// <param name="args"></param>
    //public abstract void RemoveStreamFromFile(string streamId);

    /// <summary>
    /// clients should be able to select/preview/hover one way or another their associated objects
    /// </summary>
    /// <param name="args"></param>
    public abstract void SelectClientObjects(string args);

    /// <summary>
    /// Should return a list of filters that the application supports. 
    /// </summary>
    /// <returns></returns>
    public abstract List<ISelectionFilter> GetSelectionFilters();

    /// <summary>
    /// Should return a list of receive modes that the application supports. 
    /// </summary>
    /// <returns></returns>
    public abstract List<ReceiveMode> GetReceiveModes();

    /// <summary>
    /// Return a list of custom menu items for stream cards. 
    /// </summary>
    /// <returns></returns>
    public abstract List<MenuItem> GetCustomStreamMenuItems();

    public abstract List<ISetting> GetSettings();

    #endregion
  }
}
