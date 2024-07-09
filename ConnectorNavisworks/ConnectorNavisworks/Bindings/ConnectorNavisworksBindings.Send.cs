using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Autodesk.Navisworks.Api;
using DesktopUI2;
using DesktopUI2.Models;
using DesktopUI2.Models.Settings;
using DesktopUI2.ViewModels;
using Speckle.ConnectorNavisworks.Other;
using Speckle.Core.Api;
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Speckle.Core.Transports;
using Application = Autodesk.Navisworks.Api.Application;
using Cursor = System.Windows.Forms.Cursor;

namespace Speckle.ConnectorNavisworks.Bindings;

public partial class ConnectorBindingsNavisworks
{
  private ConcurrentDictionary<string, int> _conversionProgressDict;
  private int _convertedCount;
  private ProgressInvoker _progressBar;
  private ProgressViewModel _progressViewModel;

  public override bool CanPreviewSend => false;

  // Stub - Preview send is not supported
  public override async void PreviewSend(StreamState state, ProgressViewModel progress) =>
    await Task.Delay(TimeSpan.FromMilliseconds(500)).ConfigureAwait(false);

  /// <inheritdoc />
  /// <summary>
  /// Sends the stream to Speckle.
  /// </summary>
  /// <param name="state">The stream state.</param>
  /// <param name="progress">The progress view model.</param>
  /// <returns>The ID of the commit created for the sent stream.</returns>
  /// <exception cref="OperationCanceledException">Thrown when the operation is canceled via the cancellation token.</exception>
  public override async Task<string> SendStream(StreamState state, ProgressViewModel progress)
  {
    _progressViewModel = progress;

    // Perform the validation checks - will throw if something is wrong
    ValidateBeforeSending(state);

    string commitId;
    var applicationProgress = Application.BeginProgress("Send to Speckle.");
    _progressBar = new ProgressInvoker(applicationProgress);

    SetupProgressViewModel();
    SetupConverter(state);
    _settingsHandler.InitializeManagerOptionsForSend(state);

    try
    {
      Collection commitObject;
      if (PersistCache == false || CachedConversion == false)
      {
        commitObject = CommitObject;

        // Reset the cached conversion and commit objects
        CachedConvertedElements = null;
        s_cachedState = state;
        s_cachedCommit = commitObject;

        Cursor.Current = Cursors.WaitCursor;

        _settingsHandler.DisableAutoSave();

        _progressViewModel.CancellationToken.ThrowIfCancellationRequested();

        var modelItemsToConvert = PrepareModelItemsToConvert(state);

        _progressViewModel.CancellationToken.ThrowIfCancellationRequested();

        var conversions = PrepareElementsForConversion(modelItemsToConvert);

        _progressViewModel.CancellationToken.ThrowIfCancellationRequested();

        _convertedCount = ElementAndViewsConversion(state, conversions, commitObject);

        CachedConvertedElements = commitObject.elements;

        _settingsHandler.RestoreAutoSave();

        _progressViewModel.CancellationToken.ThrowIfCancellationRequested();
      }
      else
      {
        commitObject = s_cachedCommit as Collection;
        if (commitObject != null)
        {
          commitObject.elements = CachedConvertedElements;
        }
      }

      var objectId = await SendConvertedObjectsToSpeckle(state, commitObject).ConfigureAwait(false);

      _progressViewModel.CancellationToken.ThrowIfCancellationRequested();

      commitId = await CreateCommit(state, objectId).ConfigureAwait(false);

      if (PersistCache == false)
      {
        // On success, cancel the conversion and commit object cache
        s_cachedCommit = null;
        CachedConvertedElements = null;
      }
    }
    finally
    {
      _settingsHandler.RestoreInternalPropertiesDisplay();
    }

    Cursor.Current = Cursors.Default;

    try
    {
      Application.EndProgress();
    }
    catch (InvalidOperationException)
    {
      // ignored
    }

    return commitId;
  }

  /// <summary>
  /// Prepares model items to be converted for a given stream state.
  /// </summary>
  /// <param name="state">The stream state.</param>
  /// <returns>A list of ModelItem objects that are ready to be converted.</returns>
  private List<ModelItem> PrepareModelItemsToConvert(StreamState state)
  {
    _conversionProgressDict = new ConcurrentDictionary<string, int> { ["Conversion"] = 0 };

    var modelItemsToConvert = GetModelItemsForConversion(state);

    _progressViewModel.Max = modelItemsToConvert.Count;

    return modelItemsToConvert.Where(e => e != null).ToList();
  }

  /// <summary>
  /// Handles updates to the progress of the operation.
  /// </summary>
  /// <param name="progressDict">A dictionary containing progress details.</param>
  private void HandleProgress(ConcurrentDictionary<string, int> progressDict)
  {
    // If the "RemoteTransport" key exists in the dictionary and has a positive value
    if (progressDict.TryGetValue("RemoteTransport", out var rc) && rc > 0)
    {
      // Update the progress bar proportionally to the remote conversion count
      _progressBar.Update(Math.Min((double)rc / 2 / _convertedCount, 1.0));
    }

    // Update the progress view model with the progress dictionary
    _progressViewModel.Update(progressDict);
  }

  /// <summary>
  /// Handles errors that occur during the operation.
  /// </summary>
  /// <param name="_">Unused parameter (typically the sender).</param>
  /// <param name="ex">The exception that occurred.</param>
  private void HandleError(string _, Exception ex) =>
    // Add the exception to the report's operation errors
    _progressViewModel.Report.OperationErrors.Add(ex);

  /// <summary>
  /// Sends converted objects to the Speckle server.
  /// </summary>
  /// <param name="state">The current state of the stream.</param>
  /// <param name="commitObject">The collection of objects to send.</param>
  /// <returns>The ID of the object being sent.</returns>
  private async Task<string> SendConvertedObjectsToSpeckle(StreamState state, Base commitObject)
  {
    _progressBar.EndSubOperation();
    _progressBar.BeginSubOperation(
      0.95,
      $"Pack your bags, data! That's {_convertedCount} objects going on a trip to the Speckle universe..."
    );

    _navisworksConverter.SetConverterSettings(new Dictionary<string, string> { { "_Mode", null } });

    _progressViewModel.CancellationToken.ThrowIfCancellationRequested();

    _progressViewModel.Max = _convertedCount;

    var transports = new List<ITransport> { new ServerTransport(state.Client.Account, state.StreamId) };

    var objectId = await Operations
      .Send(
        commitObject,
        _progressViewModel.CancellationToken,
        transports,
        onProgressAction: HandleProgress,
        onErrorAction: HandleError,
        disposeTransports: true
      )
      .ConfigureAwait(false);

    _progressBar.EndSubOperation();

    return objectId;
  }

  /// <summary>
  /// Creates a new commit.
  /// </summary>
  /// <param name="state">The StreamState object, contains stream details and client.</param>
  /// <param name="objectId">The id of the object to commit.</param>
  /// <returns>The id of the created commit.</returns>
  private async Task<string> CreateCommit(StreamState state, string objectId)
  {
    _progressBar.BeginSubOperation(1, "Sealing the deal... Your data's new life begins in Speckle!");

    // Define a new commit input with stream details, object ID, and commit message
    var commit = new CommitCreateInput
    {
      streamId = state.StreamId,
      objectId = objectId,
      branchName = state.BranchName,
      message = state.CommitMessage ?? $"Sent {_convertedCount} elements from {HostApplications.Navisworks.Name}.",
      sourceApplication = HostAppNameVersion
    };

    string commitId = await ConnectorHelpers
      .CreateCommit(state.Client, commit, _progressViewModel.CancellationToken)
      .ConfigureAwait(false);

    return commitId;
  }

  public async Task RetryLastConversionSend()
  {
    if (s_activeDoc == null)
    {
      return;
    }

    if (CachedConvertedElements == null || s_cachedCommit == null)
    {
      throw new SpeckleException("Cant retry last conversion: no cached conversion or commit found.");
    }

    if (s_cachedCommit is Collection commitObject)
    {
      // _isRetrying = true;

      var applicationProgress = Application.BeginProgress("Retrying that send to Speckle.");
      _progressBar = new ProgressInvoker(applicationProgress);
      _progressViewModel = new ProgressViewModel();

      commitObject.elements = CachedConvertedElements;

      var state = s_cachedState;

      _progressBar.BeginSubOperation(0.7, "Retrying cached conversion.");
      _progressBar.EndSubOperation();

      var objectId = await SendConvertedObjectsToSpeckle(state, commitObject).ConfigureAwait(false);

      if (_progressViewModel.Report.OperationErrors.Count != 0)
      {
        ConnectorHelpers.DefaultSendErrorHandler("", _progressViewModel.Report.OperationErrors.Last());
      }

      _progressViewModel.CancellationToken.ThrowIfCancellationRequested();

      state.Settings.Add(new CheckBoxSetting { Slug = "retrying", IsChecked = true });

      string commitId;
      try
      {
        commitId = await CreateCommit(state, objectId).ConfigureAwait(false);
      }
      finally
      {
        _progressBar.EndSubOperation();
        Application.EndProgress();
        Cursor.Current = Cursors.Default;
      }

      state.Settings.RemoveAll(x => x.Slug == "retrying");

      if (string.IsNullOrEmpty(commitId))
      {
        return;
      }
    }

    // nullify the cached conversion and commit on success.
    s_cachedCommit = null;

    CachedConvertedElements = null;
    // _isRetrying = false;
  }

  /// <summary>
  /// Validates the required properties before sending a stream.
  /// </summary>
  /// <param name="state">The stream state.</param>
  void ValidateBeforeSending(StreamState state)
  {
    if (_progressViewModel == null)
    {
      throw new ArgumentException("No ProgressViewModel provided.");
    }

    if (s_activeDoc.ActiveSheet == null)
    {
      throw new InvalidOperationException("Your Document is empty. Nothing to Send.");
    }

    if (state.Filter == null)
    {
      throw new InvalidOperationException("No filter provided. Nothing to Send.");
    }

    if (state.Filter.Slug == "all" || state.CommitMessage == "Sent everything")
    {
      throw new InvalidOperationException("Everything Mode is not yet implemented. Send stopped.");
    }
  }

  /// <summary>
  /// Configures the progress view model and sets up cancellation behavior.
  /// </summary>
  private void SetupProgressViewModel()
  {
    _progressViewModel.Report = new ProgressReport();

    _progressViewModel.CancellationToken.Register(() =>
    {
      try
      {
        _progressBar.Cancel();
        Application.EndProgress();
      }
      catch (InvalidOperationException)
      {
        // ignored
      }

      _settingsHandler.RestoreAutoSave();
      Cursor.Current = Cursors.Default;
    });
  }
}
