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
using static Speckle.ConnectorNavisworks.Other.Utilities;
using Application = Autodesk.Navisworks.Api.Application;
using Cursor = System.Windows.Forms.Cursor;

namespace Speckle.ConnectorNavisworks.Bindings;

public partial class ConnectorBindingsNavisworks
{
  private ConversionInvoker _conversionInvoker;
  private ConcurrentDictionary<string, int> _conversionProgressDict;
  private int _convertedCount;
  private ProgressInvoker _progressBar;
  private ProgressViewModel _progressViewModel;

  public override bool CanPreviewSend => false;

  /// <summary>
  /// Gets a new instance of a commit object with initial properties.
  /// </summary>
  private static Collection CommitObject =>
    new()
    {
      ["units"] = GetUnits(_doc),
      collectionType = "Navisworks Model",
      name = _doc.Title,
      applicationId = "Root"
    };

  // Stub - Preview send is not supported
  public override async void PreviewSend(StreamState state, ProgressViewModel progress)
  {
    await Task.Delay(TimeSpan.FromMilliseconds(500)).ConfigureAwait(false);
    // TODO!
  }

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
    InitializeManagerOptionsForSend(state);

    try
    {
      Collection commitObject;
      if (PersistCache == false || CachedConversion == false)
      {
        commitObject = CommitObject;

        // Reset the cached conversion and commit objects
        CachedConvertedElements = null;
        _cachedState = state;
        _cachedCommit = commitObject;

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
        commitObject = _cachedCommit as Collection;
        if (commitObject != null)
          commitObject.elements = CachedConvertedElements;
      }

      var objectId = await SendConvertedObjectsToSpeckle(state, commitObject).ConfigureAwait(false);

      if (_progressViewModel.Report.OperationErrors.Any())
        ConnectorHelpers.DefaultSendErrorHandler("", _progressViewModel.Report.OperationErrors.Last());

      _progressViewModel.CancellationToken.ThrowIfCancellationRequested();

      commitId = await CreateCommit(state, objectId).ConfigureAwait(false);

      if (PersistCache == false)
      {
        // On success, cancel the conversion and commit object cache
        _cachedCommit = null;
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
  /// The SettingsManager can be seeded with the options for the conversion.
  /// </summary>
  /// <param name="state"></param>
  /// <exception cref="NotImplementedException"></exception>
  private void InitializeManagerOptionsForSend(StreamState state)
  {
    var internalPropertySettings = state.Settings.Find(x => x.Slug == "internal-properties");
    var internalPropertyNames = state.Settings.Find(x => x.Slug == "internal-property-names");

    if (internalPropertySettings != null && ((CheckBoxSetting)internalPropertySettings).IsChecked)
      _settingsHandler.ShowInternalProperties();
    if (internalPropertyNames != null && ((CheckBoxSetting)internalPropertyNames).IsChecked)
      _settingsHandler.UseInternalPropertyNames();
  }

  /// <summary>
  /// Validates the required properties before sending a stream.
  /// </summary>
  /// <param name="state">The stream state.</param>
  private void ValidateBeforeSending(StreamState state)
  {
    if (_progressViewModel == null)
      throw new ArgumentException("No ProgressViewModel provided.");

    if (_doc.ActiveSheet == null)
      throw new InvalidOperationException("Your Document is empty. Nothing to Send.");

    if (state.Filter == null)
      throw new InvalidOperationException("No filter provided. Nothing to Send.");

    if (state.Filter.Slug == "all" || state.CommitMessage == "Sent everything")
      throw new InvalidOperationException("Everything Mode is not yet implemented. Send stopped.");
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

  /// <summary>
  /// Prepares the elements from the provided model items for conversion.
  /// </summary>
  /// <param name="modelItemsToConvert">The list of model items to convert.</param>
  /// <returns>A dictionary of elements ready for conversion.</returns>
  private Dictionary<Element, Tuple<Constants.ConversionState, Base>> PrepareElementsForConversion(
    IReadOnlyList<ModelItem> modelItemsToConvert
  )
  {
    _progressBar.BeginSubOperation(0.1, "Who's who? Let's check the ID cards...");
    var conversions = new Dictionary<Element, Tuple<Constants.ConversionState, Base>>();

    var totalObjects = modelItemsToConvert.Count;
    var objectIncrement = 1 / Math.Max((double)totalObjects, 1);
    var objectInterval = Math.Max(totalObjects / 100, 1);

    for (int index = 0; index < modelItemsToConvert.Count; index++)
    {
      if (_progressBar.IsCanceled)
        _progressViewModel.CancellationTokenSource.Cancel();

      _progressViewModel.CancellationToken.ThrowIfCancellationRequested();

      ModelItem modelItem = modelItemsToConvert[index];
      var element = new Element();
      element.GetElement(modelItem);
      conversions.Add(element, new Tuple<Constants.ConversionState, Base>(Constants.ConversionState.ToConvert, null));

      if (index % objectInterval == 0 || index == modelItemsToConvert.Count - 1)
        _progressBar.Update((index + 1) * objectIncrement);
    }

    _progressBar.EndSubOperation();

    return conversions;
  }

  /// <summary>
  /// Sets up the converter with the appropriate settings and context document.
  /// </summary>
  /// <param name="state">The stream state containing the settings.</param>
  private void SetupConverter(StreamState state)
  {
    _defaultKit = KitManager.GetDefaultKit();
    _navisworksConverter = _defaultKit.LoadConverter(VersionedAppName);

    CurrentSettings = state.Settings;
    var settings = state.Settings.ToDictionary(setting => setting.Slug, setting => setting.Selection);

    _navisworksConverter.SetContextDocument(_doc);
    _navisworksConverter.SetConverterSettings(settings);
    _navisworksConverter.Report.ReportObjects.Clear();
  }

  /// <summary>
  /// Prepares model items to be converted for a given stream state.
  /// </summary>
  /// <param name="state">The stream state.</param>
  /// <param name="totalObjects">Out parameter to return the total number of objects to convert.</param>
  /// <returns>A list of ModelItem objects that are ready to be converted.</returns>
  private List<ModelItem> PrepareModelItemsToConvert(StreamState state)
  {
    _conversionProgressDict = new ConcurrentDictionary<string, int> { ["Conversion"] = 0 };

    var modelItemsToConvert = GetModelItemsForConversion(state);

    _progressViewModel.Max = modelItemsToConvert.Count;

    return modelItemsToConvert.Where(e => e != null).ToList();
  }

  /// <summary>
  /// Handles conversion of elements and views.
  /// </summary>
  /// <param name="state">The current stream state.</param>
  /// <param name="conversions">Dictionary of elements to be converted.</param>
  /// <param name="commitObject">The objects to commit.</param>
  /// <returns>Count of successfully converted objects.</returns>
  private int ElementAndViewsConversion(
    StreamState state,
    IDictionary<Element, Tuple<Constants.ConversionState, Base>> conversions,
    Collection commitObject
  )
  {
    _progressBar.BeginSubOperation(0.35, "Spinning the alchemy wheel, transmuting data...");
    _navisworksConverter.SetConverterSettings(new Dictionary<string, string> { { "_Mode", "objects" } });
    _conversionInvoker = new ConversionInvoker(_navisworksConverter);
    var converted = ConvertObjects(conversions);
    var convertedCount = converted.Count(x => x.Value.Item1 == Constants.ConversionState.Converted);

    if (convertedCount == 0)
    {
      _settingsHandler.RestoreAutoSave();
      throw new SpeckleException("Zero objects converted successfully. Send stopped.");
    }

    _progressBar.StartNewSubOperation(0.2, "Building a family tree, data-style...");

    commitObject.elements = Element.BuildNestedObjectHierarchy(converted, state).ToList();

    if (commitObject.elements.Count == 0)
    {
      _settingsHandler.RestoreAutoSave();
      throw new SpeckleException(
        "All Geometry objects in the selection are hidden or cannot be converted. Send stopped."
      );
    }

    _progressViewModel.Report.Merge(_navisworksConverter.Report);

    _progressBar.StartNewSubOperation(0.1, "Sending Views.");
    ConvertViews(state, commitObject);
    _progressBar.EndSubOperation();

    return convertedCount;
  }

  /// <summary>
  /// Handles updates to the progress of the operation.
  /// </summary>
  /// <param name="progressDict">A dictionary containing progress details.</param>
  /// <param name="convertedCount">The total number of converted items.</param>
  private void HandleProgress(ConcurrentDictionary<string, int> progressDict)
  {
    // If the "RemoteTransport" key exists in the dictionary and has a positive value
    if (progressDict.TryGetValue("RemoteTransport", out var rc) && rc > 0)
      // Update the progress bar proportionally to the remote conversion count
      _progressBar.Update(Math.Min((double)rc / 2 / _convertedCount, 1.0));

    // Update the progress view model with the progress dictionary
    _progressViewModel.Update(progressDict);
  }

  /// <summary>
  /// Handles errors that occur during the operation.
  /// </summary>
  /// <param name="_">Unused parameter (typically the sender).</param>
  /// <param name="ex">The exception that occurred.</param>
  private void HandleError(string _, Exception ex)
  {
    // Add the exception to the report's operation errors
    _progressViewModel.Report.OperationErrors.Add(ex);
  }

  /// <summary>
  /// Sends converted objects to the Speckle server.
  /// </summary>
  /// <param name="state">The current state of the stream.</param>
  /// <param name="commitObject">The collection of objects to send.</param>
  /// <param name="convertedCount">The total number of converted items.</param>
  /// <returns>The ID of the sent object.</returns>
  private async Task<string> SendConvertedObjectsToSpeckle(StreamState state, Base commitObject)
  {
    _progressBar.BeginSubOperation(
      0.3,
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
  /// <param name="convertedCount">The count of converted elements.</param>
  /// <returns>The id of the created commit.</returns>
  private async Task<string> CreateCommit(StreamState state, string objectId)
  {
    _progressBar.BeginSubOperation(0.1, "Sealing the deal... Your data's new life begins in Speckle!");

    // Define a new commit input with stream details, object ID, and commit message
    var commit = new CommitCreateInput
    {
      streamId = state.StreamId,
      objectId = objectId,
      branchName = state.BranchName,
      message = state.CommitMessage ?? $"Sent {_convertedCount} elements from {HostApplications.Navisworks.Name}.",
      sourceApplication = HostAppNameVersion
    };

    string commitId =
    // This block enables forcing a failed send to test the caching feature
    // #if DEBUG
    //     if (!isRetrying)
    //       throw new SpeckleException("Debug mode: commit not created.");
    // #endif
    // Use the helper function to create the commit and retrieve the commit ID
    await ConnectorHelpers
      .CreateCommit(state.Client, commit, _progressViewModel.CancellationToken)
      .ConfigureAwait(false);

    return commitId;
  }

  /// <summary>
  /// Retrieves a list of ModelItems from the provided stream state. This method checks the selection filter,
  /// populates hierarchy, omits hidden items, and verifies the visibility of the objects for conversion.
  /// </summary>
  /// <param name="state">The current stream state.</param>
  /// <returns>A list of model items to convert.</returns>
  /// <exception cref="InvalidOperationException">Thrown when no objects are selected or visible for conversion.</exception>
  private List<ModelItem> GetModelItemsForConversion(StreamState state)
  {
    var modelItemsToConvert = new List<ModelItem>();

    var selectionBuilder = new SelectionHandler(state, _progressViewModel) { ProgressBar = _progressBar };

    selectionBuilder.GetFromFilter();

    selectionBuilder.ValidateStartNodes();

    // Check if any items have been selected
    if (selectionBuilder.Count == 0)
      throw new InvalidOperationException(
        "Zero objects selected; send stopped. Please select some objects, or check that your filter can actually select something."
      );

    try
    {
      selectionBuilder.PopulateHierarchyAndOmitHidden();
    }
    catch
    {
      throw new InvalidOperationException("An error occurred retrieving objects from your saved selection source.");
    }

    modelItemsToConvert.AddRange(selectionBuilder.ModelItems);

    if (!modelItemsToConvert.Any())
      throw new InvalidOperationException(
        "Zero objects visible for conversion; send stopped. Please select some objects, or check that your filter can actually select something."
      );

    return modelItemsToConvert;
  }

  /// <summary>
  /// Converts Navisworks views into Base objects and adds them to a given commit object.
  /// This includes selected views from a provided filter and, optionally, the active and home views.
  /// </summary>
  /// <param name="state">The current stream state, containing filter data.</param>
  /// <param name="commitObject">The object to which converted views should be added.</param>
  private void ConvertViews(StreamState state, DynamicBase commitObject)
  {
    var views = new List<Base>();

    _navisworksConverter.SetConverterSettings(new Dictionary<string, string> { { "_Mode", "views" } });

    if (state.Filter?.Slug == "views")
    {
      var selectionBuilder = new SelectionHandler(state, _progressViewModel) { ProgressBar = _progressBar };

      var selectedViews = state.Filter.Selection
        .Distinct()
        .Select(selectionBuilder.ResolveSavedViewpoint)
        .Select(_conversionInvoker.Convert)
        .Where(c => c != null)
        .ToList();

      views.AddRange(selectedViews);
    }
    // Only send current view if we aren't sending other views.
    else if (CurrentSettings.Find(x => x.Slug == "current-view") is CheckBoxSetting { IsChecked: true })
    {
      var currentView = _conversionInvoker.Convert(_doc.CurrentViewpoint.ToViewpoint());
      var homeView = _conversionInvoker.Convert(_doc.HomeView);

      if (currentView != null)
      {
        currentView["name"] = "Active View";
        views.Add(currentView);
      }

      if (homeView != null)
      {
        homeView["name"] = "Home View";
        views.Add(homeView);
      }
    }

    if (views.Any())
      commitObject["views"] = views;
  }

  /// <summary>
  /// Converts a set of Navisworks elements into Speckle objects and logs their conversion status.
  /// </summary>
  /// <param name="conversions">A dictionary mapping elements to their conversion states and corresponding Base objects.</param>
  /// <returns>The number of successfully converted elements.</returns>
  private Dictionary<Element, Tuple<Constants.ConversionState, Base>> ConvertObjects(
    IDictionary<Element, Tuple<Constants.ConversionState, Base>> allConversions
  )
  {
    Dictionary<Element, Tuple<Constants.ConversionState, Base>> conversions = allConversions
      .Where(c => c.Key != null && c.Value.Item1 == Constants.ConversionState.ToConvert)
      .ToDictionary(c => c.Key, c => c.Value);

    int convertedCount = 0;
    var conversionIncrement = conversions.Count != 0 ? 1.0 / conversions.Count : 0.0;
    var conversionInterval = Math.Max(conversions.Count / 100, 1);

    for (var i = 0; i < conversions.Count; i++)
    {
      if (_progressBar.IsCanceled)
        _progressViewModel.CancellationTokenSource.Cancel();

      _progressViewModel.CancellationToken.ThrowIfCancellationRequested();

      var nextToConvert = conversions.ElementAt(i);
      var element = nextToConvert.Key;

      // Get the descriptor of the element
      var descriptor = element.Descriptor();

      if (_navisworksConverter.Report.ReportObjects.TryGetValue(element.PseudoId, out var applicationObject))
      {
        _progressViewModel.Report.Log(applicationObject);
        conversions[element] = new Tuple<Constants.ConversionState, Base>(
          Constants.ConversionState.Converted,
          nextToConvert.Value.Item2
        );
        continue;
      }

      var reportObject = new ApplicationObject(element.PseudoId, descriptor) { applicationId = element.PseudoId };

      if (!_navisworksConverter.CanConvertToSpeckle(element.ModelItem))
      {
        reportObject.Update(
          status: ApplicationObject.State.Skipped,
          logItem: "Sending this object type is not supported in Navisworks"
        );
        _progressViewModel.Report.Log(reportObject);
        conversions[element] = new Tuple<Constants.ConversionState, Base>(
          Constants.ConversionState.Skipped,
          nextToConvert.Value.Item2
        );
        continue;
      }

      _navisworksConverter.Report.Log(reportObject);

      // Convert the model item to Speckle
      var converted = _conversionInvoker.Convert(element.ModelItem);

      if (converted == null)
      {
        reportObject.Update(status: ApplicationObject.State.Failed, logItem: "Conversion returned null");
        _progressViewModel.Report.Log(reportObject);
        conversions[element] = new Tuple<Constants.ConversionState, Base>(
          Constants.ConversionState.Failed,
          nextToConvert.Value.Item2
        );
        continue;
      }

      converted.applicationId = element.PseudoId;
      conversions[element] = new Tuple<Constants.ConversionState, Base>(Constants.ConversionState.Converted, converted);
      convertedCount++;
      _conversionProgressDict["Conversion"] = convertedCount;
      _progressViewModel.Update(_conversionProgressDict);
      reportObject.Update(status: ApplicationObject.State.Created, logItem: $"Sent as {converted.speckle_type}");
      _progressViewModel.Report.Log(reportObject);

      if (i % conversionInterval != 0 && i != conversions.Count)
        continue;

      double progress = (i + 1) * conversionIncrement;
      _progressBar.Update(progress);
    }

    return conversions;
  }
}
