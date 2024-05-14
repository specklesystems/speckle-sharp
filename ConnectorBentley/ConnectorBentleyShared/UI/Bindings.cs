using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Bentley.DgnPlatformNET;
using Bentley.DgnPlatformNET.Elements;
using Bentley.MstnPlatformNET;
using DesktopUI2;
using DesktopUI2.Models;
using DesktopUI2.Models.Filters;
using DesktopUI2.Models.Settings;
using DesktopUI2.ViewModels;
using Speckle.ConnectorBentley.Storage;
using Speckle.Core.Api;
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Speckle.Core.Transports;
#if (OPENBUILDINGS)
using Bentley.Building.Api;
#endif

#if (OPENROADS || OPENRAIL)
using Bentley.CifNET.GeometryModel.SDK;
using Bentley.CifNET.LinearGeometry;
using Bentley.CifNET.SDK;
#endif

namespace Speckle.ConnectorBentley.UI;

public partial class ConnectorBindingsBentley : ConnectorBindings
{
  public DgnFile File => Session.Instance.GetActiveDgnFile();
  public DgnModel Model => Session.Instance.GetActiveDgnModel();
  public string ModelUnits { get; set; }
  public List<StreamState> DocumentStreams { get; set; } = new List<StreamState>();
#if (OPENROADS || OPENRAIL)
  public GeometricModel GeomModel { get; private set; }
  public List<string> civilElementKeys => new() { "Alignment" };
#endif

#if (OPENBUILDINGS)
  public bool ExportGridLines { get; set; } = true;
#else
  public bool ExportGridLines = false;
#endif

  // Like the AutoCAD API, the Bentley APIs should only be called on the main thread.
  // As in the AutoCAD/Civil3D connectors, we therefore creating a control in the ConnectorBindings constructor (since it's called on main thread) that allows for invoking worker threads on the main thread - thank you Claire!!
  public System.Windows.Forms.Control Control;
  delegate void SetContextDelegate(object session);
  delegate List<string> GetObjectsFromFilterDelegate(
    ISelectionFilter filter,
    ISpeckleConverter converter,
    ProgressViewModel progress
  );
  delegate Base SpeckleConversionDelegate(object commitObject);

  public ConnectorBindingsBentley()
    : base()
  {
    Control = new System.Windows.Forms.Control();
    Control.CreateControl();

    ModelUnits = Model.GetModelInfo().GetMasterUnit().GetName(true, true);

#if (OPENROADS || OPENRAIL)
    ConsensusConnection sdkCon = Bentley.CifNET.SDK.Edit.ConsensusConnectionEdit.GetActive();
    GeomModel = sdkCon.GetActiveGeometricModel();
#endif
  }

  #region local streams
  public override void WriteStreamsToFile(List<StreamState> streams)
  {
    StreamStateManager.WriteStreamStateList(File, streams);
  }

  public override List<StreamState> GetStreamsInFile()
  {
    var streams = new List<StreamState>();
    if (File != null)
    {
      streams = StreamStateManager.ReadState(File);
    }

    return streams;
  }
  #endregion

  #region boilerplate
  public override string GetHostAppNameVersion() => Utils.VersionedAppName;

  public override string GetHostAppName() => Utils.Slug;

  public override string GetDocumentId()
  {
    string path = GetDocumentLocation();
    return Core.Models.Utilities.HashString(
      path + File.GetFileName(),
      Speckle.Core.Models.Utilities.HashingFunctions.MD5
    );
  }

  public override string GetDocumentLocation() => Path.GetDirectoryName(File.GetFileName());

  public override string GetFileName() => Path.GetFileName(File.GetFileName());

  public override string GetActiveViewName() => "Entire Document";

  public override List<string> GetObjectsInView()
  {
    if (Model == null)
    {
      return new List<string>();
    }

    var graphicElements = Model.GetGraphicElements();

    var objs = new List<string>();
    using (var elementEnumerator = (ModelElementsEnumerator)graphicElements.GetEnumerator())
    {
      objs = graphicElements.Where(el => !el.IsInvisible).Select(el => el.ElementId.ToString()).ToList(); // Note: this returns all graphic objects in the model.
    }

    return objs;
  }

  public override List<string> GetSelectedObjects()
  {
    var objs = new List<string>();

    if (Model == null)
    {
      return objs;
    }

    uint numSelected = SelectionSetManager.NumSelected();
    DgnModelRef modelRef = Session.Instance.GetActiveDgnModelRef();

    for (uint i = 0; i < numSelected; i++)
    {
      Bentley.DgnPlatformNET.Elements.Element el = null;
      SelectionSetManager.GetElement(i, ref el, ref modelRef);
      objs.Add(el.ElementId.ToString());
    }

    return objs;
  }

  public override List<ISelectionFilter> GetSelectionFilters()
  {
    //Element Type, Element Class, Element Template, Material, Level, Color, Line Style, Line Weight
    var levels = new List<string>();
    FileLevelCache levelCache = Model.GetFileLevelCache();
    foreach (var level in levelCache.GetHandles())
    {
      levels.Add(level.Name);
    }

    levels.Sort();

    var elementTypes = new List<string>
    {
      "Arc",
      "Ellipse",
      "Line",
      "Spline",
      "Line String",
      "Complex Chain",
      "Shape",
      "Complex Shape",
      "Mesh"
    };

    var filterList = new List<ISelectionFilter>();
    filterList.Add(
      new AllSelectionFilter
      {
        Slug = "all",
        Name = "Everything",
        Icon = "CubeScan",
        Description = "Selects all document objects."
      }
    );
    filterList.Add(
      new ListSelectionFilter
      {
        Slug = "level",
        Name = "Levels",
        Icon = "LayersTriple",
        Description = "Selects objects based on their level.",
        Values = levels
      }
    );
    filterList.Add(
      new ListSelectionFilter
      {
        Slug = "elementType",
        Name = "Element Types",
        Icon = "Category",
        Description = "Selects objects based on their element type.",
        Values = elementTypes
      }
    );

#if (OPENROADS || OPENRAIL)
    var civilElementTypes = new List<string> { "Alignment" };
    filterList.Add(
      new ListSelectionFilter
      {
        Slug = "civilElementType",
        Name = "Civil Features",
        Icon = "RailroadVariant",
        Description = "Selects civil features based on their type.",
        Values = civilElementTypes
      }
    );
#endif

    return filterList;
  }

  public override List<ReceiveMode> GetReceiveModes()
  {
    return new List<ReceiveMode> { ReceiveMode.Create };
  }

  public override List<ISetting> GetSettings()
  {
    return new List<ISetting>();
  }

  //TODO
  public override List<MenuItem> GetCustomStreamMenuItems()
  {
    return new List<MenuItem>();
  }

  public override void SelectClientObjects(List<string> args, bool deselect = false)
  {
    // TODO!
  }
  #endregion

  #region receiving
  public override bool CanPreviewReceive => false;

  public override Task<StreamState> PreviewReceive(StreamState state, ProgressViewModel progress)
  {
    return null;
  }

  public override async Task<StreamState> ReceiveStream(StreamState state, ProgressViewModel progress)
  {
    var kit = KitManager.GetDefaultKit();
    var converter = kit.LoadConverter(Utils.VersionedAppName);
    var previouslyReceivedObjects = state.ReceivedObjects;

    if (Control.InvokeRequired)
    {
      Control.Invoke(new SetContextDelegate(converter.SetContextDocument), new object[] { Session.Instance });
    }
    else
    {
      converter.SetContextDocument(Session.Instance);
    }

    progress.CancellationToken.ThrowIfCancellationRequested();

    /*
    if (Doc == null)
    {
      throw new OperationInvalidException($"No Document is open."));
    }
    */

    //if "latest", always make sure we get the latest commit when the user clicks "receive"
    Commit commit = await ConnectorHelpers.GetCommitFromState(state, progress.CancellationToken);
    state.LastCommit = commit;
    Base commitObject = await ConnectorHelpers.ReceiveCommit(commit, state, progress);
    await ConnectorHelpers.TryCommitReceived(state, commit, Utils.VersionedAppName, progress.CancellationToken);

    // invoke conversions on the main thread via control
    int count = 0;
    var flattenedObjects = FlattenCommitObject(commitObject, converter, ref count);
    List<ApplicationObject> newPlaceholderObjects;
    if (Control.InvokeRequired)
    {
      newPlaceholderObjects =
        (List<ApplicationObject>)
          Control.Invoke(
            new NativeConversionAndBakeDelegate(ConvertAndBakeReceivedObjects),
            new object[] { flattenedObjects, converter, state, progress }
          );
    }
    else
    {
      newPlaceholderObjects = ConvertAndBakeReceivedObjects(flattenedObjects, converter, state, progress);
    }

    DeleteObjects(previouslyReceivedObjects, newPlaceholderObjects);

    state.ReceivedObjects = newPlaceholderObjects;

    progress.Report.Merge(converter.Report);

    if (progress.Report.OperationErrorsCount != 0)
    {
      return null; // the commit is being rolled back
    }

    try
    {
      //await state.RefreshStream();
      WriteStateToFile();
    }
    catch (Exception e)
    {
      progress.Report.OperationErrors.Add(e);
    }

    return state;
  }

  delegate List<ApplicationObject> NativeConversionAndBakeDelegate(
    List<Base> objects,
    ISpeckleConverter converter,
    StreamState state,
    ProgressViewModel progress
  );

  private List<ApplicationObject> ConvertAndBakeReceivedObjects(
    List<Base> objects,
    ISpeckleConverter converter,
    StreamState state,
    ProgressViewModel progress
  )
  {
    var placeholders = new List<ApplicationObject>();
    var conversionProgressDict = new ConcurrentDictionary<string, int>();
    conversionProgressDict["Conversion"] = 0;
    progress.Max = state.SelectedObjectIds.Count();
    Action updateProgressAction = () =>
    {
      conversionProgressDict["Conversion"]++;
      progress.Update(conversionProgressDict);
    };

    foreach (var @base in objects)
    {
      progress.CancellationToken.ThrowIfCancellationRequested();

      try
      {
        var convRes = converter.ConvertToNative(@base);

        if (convRes is ApplicationObject placeholder)
        {
          placeholders.Add(placeholder);
        }
        else if (convRes is List<ApplicationObject> placeholderList)
        {
          placeholders.AddRange(placeholderList);
        }

        // creating new elements, not updating existing!
        var convertedElement = convRes as Element;
        if (convertedElement != null)
        {
          var status = convertedElement.AddToModel();
          if (status == StatusInt.Error)
          {
            converter.Report.LogConversionError(
              new Exception($"Failed to bake object {@base.id} of type {@base.speckle_type}.")
            );
          }
        }
        else
        {
          converter.Report.LogConversionError(
            new Exception($"Failed to convert object {@base.id} of type {@base.speckle_type}.")
          );
        }
      }
      catch (Exception e)
      {
        converter.Report.LogConversionError(e);
      }
    }

    return placeholders;
  }

  /// <summary>
  /// Recurses through the commit object and flattens it
  /// </summary>
  /// <param name="obj"></param>
  /// <param name="converter"></param>
  /// <param name="count"></param>
  /// <param name="foundConvertibleMember"></param>
  /// <returns></returns>
  private List<Base> FlattenCommitObject(
    object obj,
    ISpeckleConverter converter,
    ref int count,
    bool foundConvertibleMember = false
  )
  {
    List<Base> objects = new();

    if (obj is Base @base)
    {
      if (converter.CanConvertToNative(@base))
      {
        objects.Add(@base);
        return objects;
      }
      else
      {
        List<string> props = @base.GetDynamicMembers().ToList();
        if (@base.GetMembers().ContainsKey("displayValue"))
        {
          props.Add("displayValue");
        }

        if (@base.GetMembers().ContainsKey("elements")) // this is for builtelements like roofs, walls, and floors.
        {
          props.Add("elements");
        }

        int totalMembers = props.Count;

        foreach (var prop in props)
        {
          count++;

          var nestedObjects = FlattenCommitObject(@base[prop], converter, ref count, foundConvertibleMember);
          if (nestedObjects.Count > 0)
          {
            objects.AddRange(nestedObjects);
            foundConvertibleMember = true;
          }
        }

        if (!foundConvertibleMember && count == totalMembers) // this was an unsupported geo
        {
          converter.Report.Log($"Skipped not supported type: {@base.speckle_type}. Object {@base.id} not baked.");
        }

        return objects;
      }
    }

    if (obj is IReadOnlyList<object> list)
    {
      count = 0;
      foreach (var listObj in list)
      {
        objects.AddRange(FlattenCommitObject(listObj, converter, ref count));
      }

      return objects;
    }

    if (obj is IDictionary dict)
    {
      count = 0;
      foreach (DictionaryEntry kvp in dict)
      {
        objects.AddRange(FlattenCommitObject(kvp.Value, converter, ref count));
      }

      return objects;
    }

    return objects;
  }

  //delete previously sent object that are no longer in this stream
  private void DeleteObjects(
    List<ApplicationObject> previouslyReceiveObjects,
    List<ApplicationObject> newPlaceholderObjects
  )
  {
    foreach (var obj in previouslyReceiveObjects)
    {
      if (newPlaceholderObjects.Any(x => x.applicationId == obj.applicationId))
      {
        continue;
      }

      // get the model object from id
      ulong id = Convert.ToUInt64(obj.CreatedIds.FirstOrDefault());
      var element = Model.FindElementById((ElementId)id);
      if (element != null)
      {
        element.DeleteFromModel();
      }
    }
  }
  #endregion

  #region sending
  public override bool CanPreviewSend => false;

  public override void PreviewSend(StreamState state, ProgressViewModel progress)
  {
    return;
  }

  public override async Task<string> SendStream(StreamState state, ProgressViewModel progress)
  {
    var kit = KitManager.GetDefaultKit();
    var converter = kit.LoadConverter(Utils.VersionedAppName);
    var streamId = state.StreamId;
    var client = state.Client;

    if (Control.InvokeRequired)
    {
      Control.Invoke(new SetContextDelegate(converter.SetContextDocument), new object[] { Session.Instance });
    }
    else
    {
      converter.SetContextDocument(Session.Instance);
    }

    var selectedObjects = new List<Object>();

    if (state.Filter != null)
    {
      if (Control.InvokeRequired)
      {
        state.SelectedObjectIds =
          (List<string>)
            Control.Invoke(
              new GetObjectsFromFilterDelegate(GetObjectsFromFilter),
              new object[] { state.Filter, converter, progress }
            );
      }
      else
      {
        state.SelectedObjectIds = GetObjectsFromFilter(state.Filter, converter, progress);
      }
    }

    if (state.SelectedObjectIds.Count == 0 && !ExportGridLines)
    {
      throw new InvalidOperationException(
        "Zero objects selected; send stopped. Please select some objects, or check that your filter can actually select something."
      );
    }

    var commitObj = new Base();

    var units = Units.GetUnitsFromString(ModelUnits).ToLower();
    commitObj["units"] = units;

    var conversionProgressDict = new ConcurrentDictionary<string, int>();
    conversionProgressDict["Conversion"] = 0;
    progress.Max = state.SelectedObjectIds.Count();
    int convertedCount = 0;

    // grab elements from active model
    var objs = new List<Element>();
#if (OPENROADS || OPENRAIL)
    bool convertCivilObject = false;
    var civObjs = new List<NamedModelEntity>();

    if (civilElementKeys.Count(x => state.SelectedObjectIds.Contains(x)) > 0)
    {
      if (Control.InvokeRequired)
      {
        civObjs =
          (List<NamedModelEntity>)Control.Invoke(new GetCivilObjectsDelegate(GetCivilObjects), new object[] { state });
      }
      else
      {
        civObjs = GetCivilObjects(state);
      }

      objs = civObjs.Select(x => x.Element).ToList();
      convertCivilObject = true;
    }
    else
    {
      objs = state.SelectedObjectIds.Select(x => Model.FindElementById((ElementId)Convert.ToUInt64(x))).ToList();
    }
#else
    objs = state.SelectedObjectIds.Select(x => Model.FindElementById((ElementId)Convert.ToUInt64(x))).ToList();
#endif

#if (OPENBUILDINGS)
    if (ExportGridLines)
    {
      var converted = ConvertGridLines(converter, progress);

      if (converted == null)
      {
        progress.Report.LogConversionError(new Exception($"Failed to convert Gridlines."));
      }
      else
      {
        var containerName = "Grid Systems";

        if (commitObj[$"@{containerName}"] == null)
        {
          commitObj[$"@{containerName}"] = new List<Base>();
        }

        ((List<Base>)commitObj[$"@{containerName}"]).Add(converted);

        // not sure this makes much sense here
        conversionProgressDict["Conversion"]++;
        progress.Update(conversionProgressDict);

        convertedCount++;
      }
    }
#endif

    foreach (var obj in objs)
    {
      progress.CancellationToken.ThrowIfCancellationRequested();

      if (obj == null)
      {
        progress.Report.Log($"Skipped not found object.");
        continue;
      }

      var objId = obj.ElementId.ToString();
      var objType = obj.ElementType;

      if (!converter.CanConvertToSpeckle(obj))
      {
        progress.Report.Log($"Skipped not supported type: ${objType}. Object ${objId} not sent.");
        continue;
      }

      // convert obj
      Base converted = null;
      string containerName = string.Empty;
      try
      {
        var levelCache = Model.GetFileLevelCache();
        var objLevel = levelCache.GetLevel(obj.LevelId);
        var layerName = "Unknown";
        if (objLevel != null)
        {
          layerName = objLevel.Name;
        }

#if (OPENROADS || OPENRAIL)
        if (convertCivilObject)
        {
          var civilObj = civObjs[objs.IndexOf(obj)];
          if (Control.InvokeRequired)
          {
            converted = (Base)
              Control.Invoke(new SpeckleConversionDelegate(converter.ConvertToSpeckle), new object[] { civilObj });
            Control.Invoke(
              (Action)(
                () =>
                {
                  containerName = civilObj.Name == "" ? "Unnamed" : civilObj.Name;
                }
              )
            );
          }
          else
          {
            converted = converter.ConvertToSpeckle(civilObj);
            containerName = civilObj.Name == "" ? "Unnamed" : civilObj.Name;
          }
        }
        else
        {
          if (Control.InvokeRequired)
          {
            converted = (Base)
              Control.Invoke(new SpeckleConversionDelegate(converter.ConvertToSpeckle), new object[] { obj });
          }
          else
          {
            converted = converter.ConvertToSpeckle(obj);
          }

          containerName = layerName;
        }
#else
        if (Control.InvokeRequired)
        {
          converted = (Base)
            Control.Invoke(new SpeckleConversionDelegate(converter.ConvertToSpeckle), new object[] { obj });
        }
        else
        {
          converted = converter.ConvertToSpeckle(obj);
        }

        containerName = layerName;
#endif
        if (converted == null)
        {
          progress.Report.LogConversionError(new Exception($"Failed to convert object {objId} of type {objType}."));
          continue;
        }
      }
      catch
      {
        progress.Report.LogConversionError(new Exception($"Failed to convert object {objId} of type {objType}."));
        continue;
      }

      /* TODO: adding the feature data and properties per object
      foreach (var key in obj.ExtensionDictionary)
      {
        converted[key] = obj.ExtensionDictionary.GetUserString(key);
      }
      */

      if (commitObj[$"@{containerName}"] == null)
      {
        commitObj[$"@{containerName}"] = new List<Base>();
      }

      ((List<Base>)commitObj[$"@{containerName}"]).Add(converted);

      conversionProgressDict["Conversion"]++;
      progress.Update(conversionProgressDict);

      converted.applicationId = objId;

      convertedCount++;
    }

    progress.Report.Merge(converter.Report);

    if (convertedCount == 0)
    {
      throw new SpeckleException("Zero objects converted successfully. Send stopped.");
    }

    progress.CancellationToken.ThrowIfCancellationRequested();

    progress.Max = convertedCount;

    var transports = new List<ITransport>() { new ServerTransport(client.Account, streamId) };

    var commitObjId = await Operations.Send(
      commitObj,
      progress.CancellationToken,
      transports,
      onProgressAction: dict => progress.Update(dict),
      onErrorAction: ConnectorHelpers.DefaultSendErrorHandler,
      disposeTransports: true
    );
    var actualCommit = new CommitCreateInput
    {
      streamId = streamId,
      objectId = commitObjId,
      branchName = state.BranchName,
      message =
        state.CommitMessage != null ? state.CommitMessage : $"Pushed {convertedCount} elements from {Utils.AppName}.",
      sourceApplication = Utils.VersionedAppName
    };

    if (state.PreviousCommitId != null)
    {
      actualCommit.parents = new List<string>() { state.PreviousCommitId };
    }

    var commitId = await ConnectorHelpers.CreateCommit(client, actualCommit, progress.CancellationToken);
    return commitId;
  }

#if (OPENROADS || OPENRAIL)
  delegate List<NamedModelEntity> GetCivilObjectsDelegate(StreamState state);

  private List<NamedModelEntity> GetCivilObjects(StreamState state)
  {
    var civilObjs = new List<NamedModelEntity>();
    foreach (var objId in state.SelectedObjectIds)
    {
      switch (objId)
      {
        case "Alignment":
          civilObjs.AddRange(GeomModel.Alignments);
          break;
        case "Corridor":
          civilObjs.AddRange(GeomModel.Corridors);
          break;
      }
    }
    return civilObjs;
  }
#endif
#if (OPENBUILDINGS)
  private Base ConvertGridLines(ISpeckleConverter converter, ProgressViewModel progress)
  {
    Base converted = null;

    ITFApplication appInst = new TFApplicationList();
    if (0 == appInst.GetProject(0, out ITFLoadableProjectList projList) && projList != null)
    {
      ITFLoadableProject proj = projList.AsTFLoadableProject;
      if (null == proj)
      {
        progress.Report.ConversionErrors.Add(new Exception("Could not retrieve project for exporting gridlines"));
        return converted;
      }

      ITFDrawingGrid drawingGrid = null;
      if (Control.InvokeRequired)
      {
        Control.Invoke(
          (Action)(
            () =>
            {
              proj.GetDrawingGrid(false, 0, out drawingGrid);
            }
          )
        );
      }
      else
      {
        proj.GetDrawingGrid(false, 0, out drawingGrid);
      }

      if (null == drawingGrid)
      {
        progress.Report.ConversionErrors.Add(new Exception("Could not retrieve drawing grid for exporting gridlines"));
        return converted;
      }

      if (Control.InvokeRequired)
      {
        converted = (Base)
          Control.Invoke(new SpeckleConversionDelegate(converter.ConvertToSpeckle), new object[] { drawingGrid });
      }
      else
      {
        converted = converter.ConvertToSpeckle(drawingGrid);
      }
    }
    return converted;
  }
#endif

  private List<string> GetObjectsFromFilter(
    ISelectionFilter filter,
    ISpeckleConverter converter,
    ProgressViewModel progress
  )
  {
    var selection = new List<string>();
    switch (filter.Slug)
    {
      case "all":
        return Model.ConvertibleObjects(converter);
      case "level":
        foreach (var levelName in filter.Selection)
        {
          var levelCache = Model.GetFileLevelCache();
          var levelHandle = levelCache.GetLevelByName(levelName);
          var levelId = levelHandle.LevelId;

          var graphicElements = Model.GetGraphicElements();
          var elementEnumerator = (ModelElementsEnumerator)graphicElements.GetEnumerator();
          var objs = graphicElements.Where(el => el.LevelId == levelId).Select(el => el.ElementId.ToString()).ToList();
          selection.AddRange(objs);
        }
        return selection;
      case "elementType":
        foreach (var typeName in filter.Selection)
        {
          MSElementType selectedType = MSElementType.None;
          switch (typeName)
          {
            case "Arc":
              selectedType = MSElementType.Arc;
              break;
            case "Ellipse":
              selectedType = MSElementType.Ellipse;
              break;
            case "Line":
              selectedType = MSElementType.Line;
              break;
            case "Spline":
              selectedType = MSElementType.BsplineCurve;
              break;
            case "Line String":
              selectedType = MSElementType.LineString;
              break;
            case "Complex Chain":
              selectedType = MSElementType.ComplexString;
              break;
            case "Shape":
              selectedType = MSElementType.Shape;
              break;
            case "Complex Shape":
              selectedType = MSElementType.ComplexShape;
              break;
            case "Mesh":
              selectedType = MSElementType.MeshHeader;
              break;
            case "Surface":
              selectedType = MSElementType.BsplineSurface;
              break;
            default:
              break;
          }
          var graphicElements = Model.GetGraphicElements();
          var elementEnumerator = (ModelElementsEnumerator)graphicElements.GetEnumerator();
          var objs = graphicElements
            .Where(el => el.ElementType == selectedType)
            .Select(el => el.ElementId.ToString())
            .ToList();
          selection.AddRange(objs);
        }
        return selection;
#if (OPENROADS || OPENRAIL)
      case "civilElementType":
        foreach (var typeName in filter.Selection)
        {
          switch (typeName)
          {
            case "Alignment":
              var alignments = GeomModel.Alignments;
              if (alignments != null)
              {
                if (alignments.Count() > 0)
                {
                  selection.Add("Alignment");
                }
              }

              break;
            case "Corridor":
              var corridors = GeomModel.Corridors;
              if (corridors != null)
              {
                if (corridors.Count() > 0)
                {
                  selection.Add("Corridor");
                }
              }

              break;
            default:
              break;
          }
        }
        return selection;
#endif
      default:
        progress.Report.LogConversionError(
          new Exception(
            "Filter type is not supported in this app. Why did the developer implement it in the first place?"
          )
        );
        return selection;
    }
  }
  #endregion

  #region helper methods
  delegate void WriteStateDelegate(DgnFile File, List<StreamState> DocumentStreams);

  /// <summary>
  /// Transaction wrapper around writing the local streams to the file.
  /// </summary>
  private void WriteStateToFile()
  {
    if (Control.InvokeRequired)
    {
      Control.Invoke(
        new WriteStateDelegate(StreamStateManager.WriteStreamStateList),
        new object[] { File, DocumentStreams }
      );
    }
    else
    {
      StreamStateManager.WriteStreamStateList(File, DocumentStreams);
    }
  }

  public override void ResetDocument()
  {
    // TODO!
  }
  #endregion
}
