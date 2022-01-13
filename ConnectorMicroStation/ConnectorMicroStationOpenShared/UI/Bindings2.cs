using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Collections.Concurrent;
using System.Collections;

using Speckle.Core.Api;
using Speckle.Core.Models;
using Speckle.Core.Kits;
using Speckle.Core.Transports;
using DesktopUI2;
using DesktopUI2.Models;
using DesktopUI2.ViewModels;
using DesktopUI2.Models.Filters;
using Speckle.ConnectorMicroStationOpenRoads.Storage;

using Bentley.DgnPlatformNET;
using Bentley.DgnPlatformNET.Elements;
using Bentley.MstnPlatformNET;
using Bentley.DgnPlatformNET.DgnEC;
using Speckle.ConnectorMicroStationOpen.Entry;
using Speckle.ConnectorMicroStationOpen.Storage;

#if (OPENBUILDINGS)
using Bentley.Building.Api;
#endif

#if (OPENROADS || OPENRAIL)
using Bentley.CifNET.GeometryModel.SDK;
using Bentley.CifNET.LinearGeometry;
using Bentley.CifNET.SDK;
#endif

using Stylet;

namespace Speckle.ConnectorMicroStationOpen.UI
{
  public partial class ConnectorBindingsMicroStationOpen2 : ConnectorBindings
  {
    public DgnFile File => Session.Instance.GetActiveDgnFile();
    public DgnModel Model => Session.Instance.GetActiveDgnModel();
    public string ModelUnits { get; set; }
    public List<StreamState> DocumentStreams { get; set; } = new List<StreamState>();
#if (OPENROADS || OPENRAIL)
    public GeometricModel GeomModel { get; private set; }
    public List<string> civilElementKeys => new List<string> { "Alignment" };
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
    delegate List<string> GetObjectsFromFilterDelegate(ISelectionFilter filter, ISpeckleConverter converter);
    delegate Base SpeckleConversionDelegate(object commitObject);

    public ConnectorBindingsMicroStationOpen2() : base()
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
      StreamStateManager2.WriteStreamStateList(File, streams);
    }

    public override List<StreamState> GetStreamsInFile()
    {
      var streams = new List<StreamState>();
      if (File != null)
        streams = StreamStateManager2.ReadState(File);
      return streams;
    }
    #endregion

    #region boilerplate
    public override string GetHostAppName() => Utils.BentleyAppName;

    public override string GetDocumentId()
    {
      string path = GetDocumentLocation();
      return Core.Models.Utilities.hashString(path + File.GetFileName(), Speckle.Core.Models.Utilities.HashingFuctions.MD5);
    }

    public override string GetDocumentLocation() => Path.GetDirectoryName(File.GetFileName());

    public override string GetFileName() => Path.GetFileName(File.GetFileName());

    public override string GetActiveViewName() => "Entire Document";

    public override List<string> GetObjectsInView()
    {
      if (Model == null)
        return new List<string>();

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

      var elementTypes = new List<string> { "Arc", "Ellipse", "Line", "Spline", "Line String", "Complex Chain", "Shape", "Complex Shape", "Mesh" };

      return new List<ISelectionFilter>()
      {

      };

      var filterList = new List<ISelectionFilter>();
      filterList.Add(new ListSelectionFilter { Slug = "level", Name = "Levels", Icon = "LayersTriple", Description = "Selects objects based on their level.", Values = levels });
      filterList.Add(new ListSelectionFilter { Slug = "elementType", Name = "Element Types", Icon = "Category", Description = "Selects objects based on their element type.", Values = elementTypes });

#if (OPENROADS || OPENRAIL)
      var civilElementTypes = new List<string> { "Alignment" };
      filterList.Add(new ListSelectionFilter { Slug = "civilElementType", Name = "Civil Features", Icon = "RailroadVariant", Description = "Selects civil features based on their type.", Values = civilElementTypes });
#endif

      filterList.Add(new AllSelectionFilter { Slug = "all", Name = "All", Icon = "CubeScan", Description = "Selects all document objects." });

      return filterList;
    }

    //TODO
    public override List<MenuItem> GetCustomStreamMenuItems()
    {
      return new List<MenuItem>();
    }

    public override void SelectClientObjects(string args)
    {
      throw new NotImplementedException();
    }
    #endregion

    #region receiving
    public override async Task<StreamState> ReceiveStream(StreamState state, ProgressViewModel progress)
    {
      var kit = KitManager.GetDefaultKit();
      var converter = kit.LoadConverter(Utils.BentleyAppName);
      var transport = new ServerTransport(state.Client.Account, state.StreamId);
      var stream = await state.Client.StreamGet(state.StreamId);
      var previouslyReceivedObjects = state.ReceivedObjects;

      if (converter == null)
        throw new Exception("Could not find any Kit!");

      if (Control.InvokeRequired)
        Control.Invoke(new SetContextDelegate(converter.SetContextDocument), new object[] { Session.Instance });
      else
        converter.SetContextDocument(Session.Instance);

      if (progress.CancellationTokenSource.Token.IsCancellationRequested)
        return null;

      /*
      if (Doc == null)
      {
        progress.Report.LogOperationError(new Exception($"No Document is open."));
        progress.CancellationTokenSource.Cancel();
      }
      */

      //if "latest", always make sure we get the latest commit when the user clicks "receive"
      Commit commit = null;
      if (state.CommitId == "latest")
      {
        var res = await state.Client.BranchGet(progress.CancellationTokenSource.Token, state.StreamId, state.BranchName, 1);
        commit = res.commits.items.FirstOrDefault();
      }
      else
      {
        commit = await state.Client.CommitGet(progress.CancellationTokenSource.Token, state.StreamId, state.CommitId);
      }
      string referencedObject = commit.referencedObject;

      var commitObject = await Operations.Receive(
        referencedObject,
        progress.CancellationTokenSource.Token,
        transport,
        onProgressAction: dict => progress.Update(dict),
        onTotalChildrenCountKnown: num => Execute.PostToUIThread(() => progress.Max = num),
        onErrorAction: (message, exception) =>
        {
          progress.Report.LogOperationError(exception);
          progress.CancellationTokenSource.Cancel();
        },
        disposeTransports: true
        );

      try
      {
        await state.Client.CommitReceived(new CommitReceivedInput
        {
          streamId = stream?.id,
          commitId = commit?.id,
          message = commit?.message,
          sourceApplication = Utils.BentleyAppName
        });
      }
      catch
      {
        // Do nothing!
      }
      if (progress.Report.OperationErrorsCount != 0)
        return state;

      // invoke conversions on the main thread via control
      var flattenedObjects = FlattenCommitObject(commitObject, converter);
      List<ApplicationPlaceholderObject> newPlaceholderObjects;
      if (Control.InvokeRequired)
        newPlaceholderObjects = (List<ApplicationPlaceholderObject>)Control.Invoke(new NativeConversionAndBakeDelegate(ConvertAndBakeReceivedObjects), new object[] { flattenedObjects, converter, state, progress });
      else
        newPlaceholderObjects = ConvertAndBakeReceivedObjects(flattenedObjects, converter, state, progress);

      if (newPlaceholderObjects == null)
      {
        converter.Report.ConversionErrors.Add(new Exception("fatal error: receive cancelled by user"));
        return null;
      }

      DeleteObjects(previouslyReceivedObjects, newPlaceholderObjects);

      state.ReceivedObjects = newPlaceholderObjects;

      progress.Report.Merge(converter.Report);

      if (progress.Report.OperationErrorsCount != 0)
        return null; // the commit is being rolled back

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

    delegate List<ApplicationPlaceholderObject> NativeConversionAndBakeDelegate(List<Base> objects, ISpeckleConverter converter, StreamState state, ProgressViewModel progress);
    private List<ApplicationPlaceholderObject> ConvertAndBakeReceivedObjects(List<Base> objects, ISpeckleConverter converter, StreamState state, ProgressViewModel progress)
    {
      var placeholders = new List<ApplicationPlaceholderObject>();
      var conversionProgressDict = new ConcurrentDictionary<string, int>();
      conversionProgressDict["Conversion"] = 0;
      Execute.PostToUIThread(() => progress.Max = state.SelectedObjectIds.Count());
      Action updateProgressAction = () =>
      {
        conversionProgressDict["Conversion"]++;
        progress.Update(conversionProgressDict);
      };

      foreach (var @base in objects)
      {
        if (progress.CancellationTokenSource.Token.IsCancellationRequested)
        {
          placeholders = null;
          break;
        }

        try
        {
          var convRes = converter.ConvertToNative(@base);

          if (convRes is ApplicationPlaceholderObject placeholder)
            placeholders.Add(placeholder);
          else if (convRes is List<ApplicationPlaceholderObject> placeholderList)
            placeholders.AddRange(placeholderList);

          // creating new elements, not updating existing!
          var convertedElement = convRes as Element;
          if (convertedElement != null)
          {
            var status = convertedElement.AddToModel();
            if (status == StatusInt.Error)
              converter.Report.LogConversionError(new Exception($"Failed to bake object {@base.id} of type {@base.speckle_type}."));
          }
          else
          {
            converter.Report.LogConversionError(new Exception($"Failed to convert object {@base.id} of type {@base.speckle_type}."));
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
    /// Recurses through the commit object and flattens it. 
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="converter"></param>
    /// <returns></returns>
    private List<Base> FlattenCommitObject(object obj, ISpeckleConverter converter)
    {
      List<Base> objects = new List<Base>();

      if (obj is Base @base)
      {
        if (converter.CanConvertToNative(@base))
        {
          objects.Add(@base);

          return objects;
        }
        else
        {
          foreach (var prop in @base.GetDynamicMembers())
          {
            objects.AddRange(FlattenCommitObject(@base[prop], converter));
          }
          return objects;
        }
      }

      if (obj is List<object> list)
      {
        foreach (var listObj in list)
        {
          objects.AddRange(FlattenCommitObject(listObj, converter));
        }
        return objects;
      }

      if (obj is IDictionary dict)
      {
        foreach (DictionaryEntry kvp in dict)
        {
          objects.AddRange(FlattenCommitObject(kvp.Value, converter));
        }
        return objects;
      }

      return objects;
    }

    //delete previously sent object that are no longer in this stream
    private void DeleteObjects(List<ApplicationPlaceholderObject> previouslyReceiveObjects, List<ApplicationPlaceholderObject> newPlaceholderObjects)
    {
      foreach (var obj in previouslyReceiveObjects)
      {
        if (newPlaceholderObjects.Any(x => x.applicationId == obj.applicationId))
          continue;

        // get the model object from id               
        ulong id = Convert.ToUInt64(obj.ApplicationGeneratedId);
        var element = Model.FindElementById((ElementId)id);
        if (element != null)
        {
          element.DeleteFromModel();
        }
      }
    }
    #endregion

    #region sending
    public override async Task SendStream(StreamState state, ProgressViewModel progress)
    {
      throw new NotImplementedException();
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
        Control.Invoke(new WriteStateDelegate(StreamStateManager2.WriteStreamStateList), new object[] { File, DocumentStreams });
      else
        StreamStateManager2.WriteStreamStateList(File, DocumentStreams);
    }
    #endregion
  }
}