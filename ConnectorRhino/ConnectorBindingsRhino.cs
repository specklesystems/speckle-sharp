using Newtonsoft.Json;
using Rhino;
using Speckle.Core.Models;
using Speckle.DesktopUI;
using Speckle.DesktopUI.Utils;
using System;
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

      SelectionTimer = new Timer(500) { AutoReset = true, Enabled = true };
      SelectionTimer.Elapsed += SelectionTimer_Elapsed;
      SelectionTimer.Start();

      GetFileContextAndNotifyUI();
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
      if (e.Document == null)
      {
        return;
      }

      GetFileContextAndNotifyUI();
    }

    private void GetFileContextAndNotifyUI()
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
      return Doc.Views.ActiveView.ActiveViewport.Name;
    }

    public override string GetApplicationHostName()
    {
      return Speckle.Core.Kits.Applications.Rhino;
    }

    public override string GetDocumentId()
    {
      return Utilities.hashString("X" + Doc?.Path + Doc?.Name, Utilities.HashingFuctions.MD5);
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

    public override List<string> GetObjectsInView()
    {
      var objs = Doc.Objects.GetSelectedObjects(true, false).Where(obj => obj.Visible).Select(obj => obj.Id.ToString()).ToList();

      return objs;
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
         new ListSelectionFilter { Name = "Layers", Icon = "FilterList", Values = layers }
      };
    }

    public override Task<StreamState> ReceiveStream(StreamState state)
    {
      // TODO: implement
      return Task.Run(() => new StreamState());
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

    public override Task<StreamState> SendStream(StreamState state)
    {
      throw new NotImplementedException();
    }

    public override void UpdateStream(StreamState state)
    {
      Doc.Strings.SetString("speckle", state.Stream.id, JsonConvert.SerializeObject(state));
    }
  }
}
