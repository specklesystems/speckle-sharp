using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Navisworks.Api;
using DesktopUI2.Models;
using DesktopUI2.Models.Settings;
using Speckle.ConnectorNavisworks.Other;
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using Speckle.Core.Models;

namespace Speckle.ConnectorNavisworks.Bindings;

public partial class ConnectorBindingsNavisworks
{
  private ConversionInvoker _conversionInvoker;

  /// <summary>
  /// Configures the converter settings.
  /// </summary>
  private void ConfigureConverter()
  {
    _navisworksConverter.SetConverterSettings(new Dictionary<string, string> { { "_Mode", "objects" } });
    _conversionInvoker = new ConversionInvoker(_navisworksConverter);
  }

  /// <summary>
  /// Initializes the conversion of model items to elements.
  /// </summary>
  /// <param name="modelItemsToConvert">The list of model items to convert.</param>
  /// <param name="conversions">The dictionary to store the converted elements.</param>
  private void InitializeConversion(
    IReadOnlyList<ModelItem> modelItemsToConvert,
    Dictionary<Element, Tuple<Constants.ConversionState, Base>> conversions
  )
  {
    var totalObjects = modelItemsToConvert.Count;
    var objectIncrement = 1 / Math.Max((double)totalObjects, 1);
    var objectInterval = Math.Max(totalObjects / 100, 1);

    for (int index = 0; index < modelItemsToConvert.Count; index++)
    {
      HandleConversionCancellation();

      ModelItem modelItem = modelItemsToConvert[index];
      var element = new Element(modelItem);

      conversions.Add(element, new Tuple<Constants.ConversionState, Base>(Constants.ConversionState.ToConvert, null));

      if (index % objectInterval == 0 || index == modelItemsToConvert.Count - 1)
      {
        _progressBar.Update((index + 1) * objectIncrement);
      }
    }
  }

  /// <summary>
  /// Sets up the converter with the appropriate settings and context document.
  /// </summary>
  /// <param name="state">The stream state containing the settings.</param>
  private void SetupConverter(StreamState state)
  {
    _defaultKit = KitManager.GetDefaultKit();
    _navisworksConverter = _defaultKit.LoadConverter(SpeckleNavisworksUtilities.VersionedAppName);

    CurrentSettings = state.Settings;
    var settings = state.Settings.ToDictionary(setting => setting.Slug, setting => setting.Selection);

    _navisworksConverter.SetContextDocument(s_activeDoc);
    _navisworksConverter.SetConverterSettings(settings);
    _navisworksConverter.Report.ReportObjects.Clear();
  }

  /// <summary>
  /// Prepares the elements from the provided model items for conversion.
  /// </summary>
  /// <param name="modelItemsToConvert">The list of model items to convert.</param>
  /// <returns>A dictionary of elements that are ready for conversion.</returns>
  private Dictionary<Element, Tuple<Constants.ConversionState, Base>> PrepareElementsForConversion(
    IReadOnlyList<ModelItem> modelItemsToConvert
  )
  {
    _progressBar.BeginSubOperation(0.1, "Who's who? Let's check the ID cards...");
    var conversions = new Dictionary<Element, Tuple<Constants.ConversionState, Base>>();

    InitializeConversion(modelItemsToConvert, conversions);
    _progressBar.EndSubOperation();

    return conversions;
  }

  /// <summary>
  /// Handles the cancellation of the conversion process.
  /// </summary>
  private void HandleConversionCancellation()
  {
    if (_progressBar.IsCanceled)
    {
      _progressViewModel.CancellationTokenSource.Cancel();
    }
    _progressViewModel.CancellationToken.ThrowIfCancellationRequested();
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
    _progressBar.BeginSubOperation(0.3, $"Spinning the alchemy wheel, transmuting {conversions.Count} objects...");
    ConfigureConverter();

    var converted = ConvertObjects(conversions);
    var convertedCount = ValidateConversion(converted);

    _progressBar.EndSubOperation();

    BuildHierarchy(converted, state, commitObject);
    ValidateHierarchy(commitObject);

    // Update report with conversion results
    _progressViewModel.Report.Merge(_navisworksConverter.Report);

    ConvertViewsProcess(state, commitObject);

    // _progressBar.EndSubOperation();

    return convertedCount;
  }

  /// <summary>
  /// Validates the converted objects and handles any conversion errors.
  /// </summary>
  /// <param name="converted">The dictionary of converted objects.</param>
  /// <returns>The count of successfully converted objects.</returns>
  /// <exception cref="SpeckleException">Thrown when zero objects are converted successfully.</exception>
  private int ValidateConversion(IDictionary<Element, Tuple<Constants.ConversionState, Base>> converted)
  {
    var convertedCount = converted.Count(x => x.Value.Item1 == Constants.ConversionState.Converted);

    if (convertedCount != 0)
    {
      return convertedCount;
    }

    _settingsHandler.RestoreAutoSave();
    throw new SpeckleException("Zero objects converted successfully. Send stopped.");
  }

  /// <summary>
  /// Builds the nested object hierarchy for the converted elements.
  /// </summary>
  /// <param name="converted">The dictionary of converted objects.</param>
  /// <param name="state">The current stream state.</param>
  /// <param name="commitObject">The objects to commit.</param>
  private void BuildHierarchy(
    Dictionary<Element, Tuple<Constants.ConversionState, Base>> converted,
    StreamState state,
    Collection commitObject
  ) =>
    // _progressBar.StartNewSubOperation(0.2, "Building a family tree, data-style...");
    // commitObject.elements = Element.BuildNestedObjectHierarchy(converted, state).ToList();
    commitObject.elements = Element.BuildNestedObjectHierarchyInParallel(converted, state, _progressBar).ToList();

  /// <summary>
  /// Validates the hierarchy and ensures it contains elements.
  /// </summary>
  /// <param name="commitObject">The objects to commit.</param>
  /// <exception cref="SpeckleException">Thrown when the hierarchy contains no elements.</exception>
  private void ValidateHierarchy(Collection commitObject)
  {
    if (commitObject.elements.Count != 0)
    {
      return;
    }

    _settingsHandler.RestoreAutoSave();
    throw new SpeckleException(
      "All Geometry objects in the selection are hidden or cannot be converted. Send stopped."
    );
  }

  /// <summary>
  /// Converts the views and updates the progress bar.
  /// </summary>
  /// <param name="state">The current stream state.</param>
  /// <param name="commitObject">The objects to commit.</param>
  private void ConvertViewsProcess(StreamState state, Collection commitObject)
  {
    _progressBar.StartNewSubOperation(0.1, "Sending Views.");
    ConvertViews(state, commitObject);
  }

  /// <summary>
  /// Converts a set of Navisworks elements into Speckle objects and logs their conversion status.
  /// </summary>
  /// <param name="allConversions">A dictionary mapping elements to their conversion states and corresponding Base objects.</param>
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
      {
        _progressViewModel.CancellationTokenSource.Cancel();
      }

      _progressViewModel.CancellationToken.ThrowIfCancellationRequested();

      var nextToConvert = conversions.ElementAt(i);
      var element = nextToConvert.Key;

      // Get the descriptor of the element
      var descriptor = element.Descriptor();

      if (_navisworksConverter.Report.ReportObjects.TryGetValue(element.IndexPath, out var applicationObject))
      {
        _progressViewModel.Report.Log(applicationObject);
        conversions[element] = new Tuple<Constants.ConversionState, Base>(
          Constants.ConversionState.Converted,
          nextToConvert.Value.Item2
        );
        continue;
      }

      var reportObject = new ApplicationObject(element.IndexPath, descriptor) { applicationId = element.IndexPath };

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

      converted.applicationId = element.IndexPath;
      conversions[element] = new Tuple<Constants.ConversionState, Base>(Constants.ConversionState.Converted, converted);
      convertedCount++;
      _conversionProgressDict["Conversion"] = convertedCount;
      _progressViewModel.Update(_conversionProgressDict);
      reportObject.Update(status: ApplicationObject.State.Created, logItem: $"Sent as {converted.speckle_type}");
      _progressViewModel.Report.Log(reportObject);

      if (i % conversionInterval != 0 && i != conversions.Count)
      {
        continue;
      }

      double progress = (i + 1) * conversionIncrement;
      _progressBar.Update(progress);
    }

    return conversions;
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
    {
      throw new InvalidOperationException(
        "Zero objects selected; send stopped. Please select some objects, or check that your filter can actually select something."
      );
    }

    try
    {
      selectionBuilder.PopulateHierarchyAndOmitHidden();
    }
    catch
    {
      throw new InvalidOperationException("An error occurred retrieving objects from your saved selection source.");
    }

    modelItemsToConvert.AddRange(selectionBuilder.ModelItems);

    if (modelItemsToConvert.Count == 0)
    {
      throw new InvalidOperationException(
        "Zero objects visible for conversion; send stopped. Please select some objects, or check that your filter can actually select something."
      );
    }

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
      var currentView = _conversionInvoker.Convert(s_activeDoc.CurrentViewpoint.ToViewpoint());
      var homeView = _conversionInvoker.Convert(s_activeDoc.HomeView);

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

    if (views.Count != 0)
    {
      commitObject["views"] = views;
    }
  }
}
