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
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Timers;

namespace SpeckleRhino
{
  public partial class ConnectorBindingsRhino : ConnectorBindings
  {

    public RhinoDoc Doc { get => RhinoDoc.ActiveDoc; }

    public Timer SelectionTimer;

    public ConnectorBindingsRhino()
    {
      RhinoDoc.EndOpenDocument += RhinoDoc_EndOpenDocument;

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
      return "Entire Document"; // Note: rhino does not have views that filter objects.
    }

    public override List<string> GetObjectsInView()
    {
      var objs = Doc.Objects.GetSelectedObjects(true, false).Where(obj => obj.Visible).Select(obj => obj.Id.ToString()).ToList(); // Note: this returns all the doc objects.

      return objs;
    }

    public override string GetApplicationHostName() => Applications.Rhino;

    public override string GetDocumentId()
    {
      return Speckle.Core.Models.Utilities.hashString("X" + Doc?.Path + Doc?.Name, Speckle.Core.Models.Utilities.HashingFuctions.MD5);
    }

    public override string GetDocumentLocation() => Doc?.Path;

    public override List<StreamState> GetFileContext()
    {
      var strings = Doc?.Strings.GetEntryNames("speckle");
      if (strings == null)
      {
        return new List<StreamState>();
      }

      return strings.Select(s => JsonConvert.DeserializeObject<StreamState>(Doc.Strings.GetValue("speckle", s))).ToList();
    }

    public override string GetFileName() => Doc?.Name;


    public override List<string> GetSelectedObjects()
    {
      var objs = Doc?.Objects.GetSelectedObjects(true, false).Select(obj => obj.Id.ToString()).ToList();
      return objs;
    }

    public override List<ISelectionFilter> GetSelectionFilters()
    {
      var layers = Doc.Layers.ToList().Select(layer => layer.Name).ToList();
      return new List<ISelectionFilter>()
      {
         new ElementsSelectionFilter { Name = "Selection", Icon = "Mouse", Selection = GetSelectedObjects()},
         new ListSelectionFilter { Name = "Category", Icon = "FilterList", Values = layers, Selection = layers },
      };
    }

    public override async Task<StreamState> ReceiveStream(StreamState state)
    {
      var kit = KitManager.GetDefaultKit();
      var converter = kit.LoadConverter(Applications.Rhino);

      var myStream = await state.Client.StreamGet(state.Stream.id);
      var commit = myStream.branches.items[0].commits.items[0];

      if (state.CancellationToken.IsCancellationRequested)
      {
        return null;
      }

      var commitObject = await Operations.Receive(
        commit.referencedObject,
        state.CancellationToken,
        new ServerTransport(state.Client.Account, state.Stream.id),
        onProgressAction: d => UpdateProgress(d, state.Progress),
        onTotalChildrenCountKnown: num => Execute.PostToUIThread(() => state.Progress.Maximum = num)
        );

      var undoRecord = Doc.BeginUndoRecord($"Speckle bake operation for {myStream.name}");

      var layerName = $"{myStream.name} @ {commit.id}";
      layerName = Regex.Replace(layerName, @"[^\u0000-\u007F]+", string.Empty); // Rhino doesn't like emojis in layer names :( 
      var layerIndex = Doc.Layers.Add(layerName, System.Drawing.Color.White);

      if (layerIndex == -1)
      {
        // TODO: clear up old stuff
        // Delete all sublayers and their objects
      }

      var layer = Doc.Layers[layerIndex];

      HandleItem(commitObject, converter, layer);

      Doc.Views.Redraw();

      Doc.EndUndoRecord(undoRecord);

      return state;
    }

    private void HandleItem(object obj, ISpeckleConverter converter, Layer layer)
    {
      if (!layer.HasIndex)
      {
        layer.Index = Doc.Layers.Add(layer);
      }
      layer.Id = Doc.Layers.FindName(layer.Name).Id;

      if (obj is Base baseItem)
      {
        if (converter.CanConvertToNative(baseItem))
        {
          var converted = converter.ConvertToNative(baseItem) as Rhino.Geometry.GeometryBase;
          if (converted != null)
          {
            Doc.Objects.Add(converted, new ObjectAttributes { LayerIndex = layer.Index });
          }

          return;
        }
        else
        {

          foreach (var prop in baseItem.GetDynamicMembers())
          {
            var value = baseItem[prop];
            var layerName = "";

            if (prop.StartsWith("@"))
            {
              layerName = prop.Remove(0, 1);
            }
            else
            {
              layerName = prop;
            }

            if (!layer.HasIndex)
            {
              layer.Index = Doc.Layers.Add(layer);
            }

            layer.Id = Doc.Layers.FindName(layer.Name).Id;

            var subLayer = new Layer() { ParentLayerId = layer.Id, Color = System.Drawing.Color.Gray, Name = layerName };
            subLayer.Index = Doc.Layers.Add(subLayer);
            HandleItem(value, converter, subLayer);
          }

          return;
        }
      }

      if (obj is List<object> list)
      {
        foreach (var listObj in list)
        {
          HandleItem(listObj, converter, layer);
        }
        return;
      }

      if (obj is IDictionary dict)
      {
        foreach (DictionaryEntry kvp in dict)
        {
          HandleItem(kvp.Value, converter, layer);
        }
        return;
      }
    }

    public override void RemoveObjectsFromClient(string args)
    {
      throw new NotImplementedException();
    }

    public override void RemoveSelectionFromClient(string args)
    {
      throw new NotImplementedException();
    }

    // TODO: remark: ui should not delete streams from the server, rather just from the file?
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
        state.CancellationToken,
        transports,
        onProgressAction: dict => UpdateProgress(dict, state.Progress),
        onErrorAction: (err, exception) => { hasErrors = true; /* TODO: a wee bit nicer handling here; plus request cancellation! */ }
        );

      if (hasErrors)
      {
        return null;
      }

      var res = await client.CommitCreate(new CommitCreateInput()
      {
        streamId = streamId,
        objectId = commitObjId,
        branchName = "main",
        message = $"Pushed {objCount} elements from Rhino."
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
