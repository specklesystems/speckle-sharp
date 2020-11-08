using Newtonsoft.Json;
using Rhino;
using Rhino.DocObjects;
using Speckle.Core.Api;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using Speckle.Core.Transports;
using Speckle.DesktopUI;
using Speckle.DesktopUI.Utils;
using Stylet;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;

namespace SpeckleRhino
{
  class ConnectorBindingsRhino : ConnectorBindings
  {

    public RhinoDoc Doc { get => RhinoDoc.ActiveDoc; }

    public Timer SelectionTimer;

    public ConnectorBindingsRhino()
    {
      Rhino.RhinoDoc.EndOpenDocument += RhinoDoc_EndOpenDocument;

      SelectionTimer = new Timer(1000) { AutoReset = true, Enabled = true };
      SelectionTimer.Elapsed += SelectionTimer_Elapsed;
      SelectionTimer.Start();
    }

    private void SelectionTimer_Elapsed(object sender, ElapsedEventArgs e)
    {
      if (Doc == null)
      {
        return;
      }

      var selection = GetSelectedObjects();

      NotifyUi(new UpdateSelectionCountEvent() { SelectionCount = selection.Count });
      NotifyUi(new UpdateSelectionEvent() { ObjectIds = selection });
    }

    private void RhinoDoc_EndOpenDocument(object sender, DocumentOpenEventArgs e)
    {
      if (e.Merge)
      {
        return; // prevents triggering this on copy pastes, imports, etc.
      }

      if (e.Document == null)
      {
        return;
      }

      GetFileContextAndNotifyUI();
    }

    public void GetFileContextAndNotifyUI()
    {
      var streamStates = GetFileContext();

      var appEvent = new ApplicationEvent()
      {
        Type = ApplicationEvent.EventType.DocumentOpened,
        DynamicInfo = streamStates
      };

      NotifyUi(appEvent);
    }

    // TODO: ask izzy when this is called/used?
    public override void AddExistingStream(string args)
    {
      //throw new NotImplementedException();
    }

    public override void AddNewStream(StreamState state)
    {
      var stateee = JsonConvert.SerializeObject(state);
      Doc.Strings.SetString("speckle", state.Stream.id, JsonConvert.SerializeObject(state));
    }

    public override void AddObjectsToClient(string args)
    {
      //throw new NotImplementedException();
    }

    public override void BakeStream(string args)
    {
      //throw new NotImplementedException();
    }

    public override string GetActiveViewName()
    {
      return "Entire Document";
    }

    public override List<string> GetObjectsInView()
    {
      var objs = Doc.Objects.GetSelectedObjects(true, false).Where(obj => obj.Visible).Select(obj => obj.Id.ToString()).ToList();

      return objs;
    }

    public override string GetApplicationHostName()
    {
      return Speckle.Core.Kits.Applications.Rhino;
    }

    public override string GetDocumentId()
    {
      return Speckle.Core.Models.Utilities.hashString("X" + Doc?.Path + Doc?.Name, Speckle.Core.Models.Utilities.HashingFuctions.MD5);
    }

    public override string GetDocumentLocation()
    {
      return Doc?.Path;
    }

    public override List<StreamState> GetFileContext()
    {
      var strings = Doc.Strings.GetEntryNames("speckle");

      return strings.Select(s => JsonConvert.DeserializeObject<StreamState>(Doc.Strings.GetValue("speckle", s))).ToList();
    }

    public override string GetFileName()
    {
      return Doc?.Name;
    }



    public override List<string> GetSelectedObjects()
    {
      var objs = Doc.Objects.GetSelectedObjects(true, false).Select(obj => obj.Id.ToString()).ToList();
      return objs;
    }

    public override List<ISelectionFilter> GetSelectionFilters()
    {
      var layers = Doc.Layers.ToList().Select(layer => layer.Name).ToList();
      return new List<ISelectionFilter>()
      {
         new ElementsSelectionFilter { Name = "Selection", Icon = "Mouse", Selection = GetSelectedObjects()},
         new ListSelectionFilter { Name = "Category", Icon = "FilterList", Values = layers, Selection = new List<string>(){ } },
         new ListSelectionFilter { Name = "Layers", Icon = "FilterList", Values = layers }
      };
    }

    public override Task<StreamState> ReceiveStream(StreamState state)
    {
      // TODO: implement
      return Task.Run(() => state);
    }

    public override void RemoveObjectsFromClient(string args)
    {
      throw new NotImplementedException();
    }

    public override void RemoveSelectionFromClient(string args)
    {
      throw new NotImplementedException();
    }

    // TODO: remark: never hit as gql errors. @izzylys: ui should not delete streams from the server, rather just from the file?
    public override void RemoveStream(string streamId)
    {
      Doc.Strings.Delete("speckle", streamId);
    }

    public override void SelectClientObjects(string args)
    {
      throw new NotImplementedException();
    }

    public override async Task<StreamState> SendStream(StreamState state)
    {
      var kit = KitManager.GetDefaultKit();
      var converter = kit.LoadConverter(Applications.Rhino);

      var rhObjects = new List<RhinoObject>();
      var baseObjects = new List<Base>();
      var layerNames = new HashSet<string>();

      var commitObj = new Base();

      var units = LengthUnits.GetUnitsFromString(Doc.GetUnitSystemName(true, false, false, false));
      commitObj["units"] = units;

      int objCount = 0;

      foreach (var placeholder in state.Placeholders)
      {
        if (state.CancellationToken.IsCancellationRequested)
        {
          return null;
        }

        var obj = Doc.Objects.FindId(new Guid(placeholder.applicationId));
        if (obj == null)
        {
          continue;
        }

        var converted = converter.ConvertToSpeckle(obj.Geometry);
        if (converted == null)
        {
          continue;
        }

        foreach (var key in obj.Attributes.GetUserStrings().AllKeys)
        {
          converted[key] = obj.Attributes.GetUserString(key);
        }

        var layerName = Doc.Layers[obj.Attributes.LayerIndex].Name;

        if (commitObj[$"@{layerName}"] == null)
        {
          commitObj[$"@{layerName}"] = new List<Base>();
        }

        ((List<Base>)commitObj[$"@{layerName}"]).Add(converted);
        objCount++;
      }

      if (state.CancellationToken.IsCancellationRequested)
      {
        return null;
      }

      Execute.PostToUIThread(() => state.Progress.Maximum = objCount);

      var streamId = state.Stream.id;
      var client = state.Client;

      var transports = new List<ITransport>() { new ServerTransport(client.Account, streamId) };

      var hasErrors = false;
      var commitObjId = await Operations.Send(
        commitObj,
        transports,
        onProgressAction: dict => UpdateProgress(dict, state.Progress),
        onErrorAction: (err, exception) => { hasErrors = true; /* TODO: a wee bit nicer handling here; plus request cancellation! */ }
        );

      if (hasErrors) return null;

      var res = await client.CommitCreate(new CommitCreateInput()
      {
        streamId = streamId,
        objectId = commitObjId,
        branchName = "main",
        message = $"Added {objCount} elements from Rhino. "
      });

      state.Stream = await client.StreamGet(streamId);
      //state.Placeholders = new List<Base>(); 
      // ask izzy: confused re the demarcation between state.objects, state.placeholders, etc. seems like
      // the above clears the set selection of a stream. 
      UpdateStream(state);
      
      RaiseNotification($"{objCount} objects sent to Speckle 🦏 + 🚀");

      return state;
    }

    public override void UpdateStream(StreamState state)
    {
      var filter = state.Filter;
      var objects = new List<Base>();

      switch (state.Filter)
      {
        case ElementsSelectionFilter selFilter:
          objects = selFilter.Selection.Select(id => new Base { applicationId = id }).ToList();
          break;
        case ListSelectionFilter selFilter:
          break;
      }
      state.Placeholders = objects;
      Doc.Strings.SetString("speckle", state.Stream.id, JsonConvert.SerializeObject(state));
    }

    private void UpdateProgress(ConcurrentDictionary<string, int> dict, ProgressReport progress)
    {
      if (progress == null)
      {
        return;
      }

      Execute.PostToUIThread(() => progress.Value = dict.Values.Last());
    }
  }
}
