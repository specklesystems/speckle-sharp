using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Forms;
using ConnectorGrasshopper.Objects;
using GH_IO.Serialization;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Attributes;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using GrasshopperAsyncComponent;
using Rhino;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Speckle.Core.Models;
using Speckle.Core.Transports;
using Logging = Speckle.Core.Logging;
using Utilities = ConnectorGrasshopper.Extras.Utilities;

namespace ConnectorGrasshopper.Ops
{
  public class SendComponent : SelectKitAsyncComponentBase
  {
    public override Guid ComponentGuid => new Guid("{5E6A5A78-9E6F-4893-8DED-7EEAB63738A5}");

    protected override Bitmap Icon => Properties.Resources.Sender;
    public override bool Obsolete => true;

    public override GH_Exposure Exposure => GH_Exposure.hidden;
    public override bool CanDisableConversion => false;
    public bool AutoSend { get; set; } = false;

    public string CurrentComponentState { get; set; } = "needs_input";

    public bool UseDefaultCache { get; set; } = true;

    public double OverallProgress { get; set; } = 0;

    public bool JustPastedIn { get; set; }

    public List<StreamWrapper> OutputWrappers = new List<StreamWrapper>();

    public string BaseId { get; set; }

    public SendComponent() : base("Send", "Send", "Sends data to a Speckle server (or any other provided transport).", ComponentCategories.PRIMARY_RIBBON,
      ComponentCategories.SEND_RECEIVE)
    {
      BaseWorker = new SendComponentWorker(this);
      Attributes = new SendComponentAttributes(this);
    }


    public override bool Write(GH_IWriter writer)
    {
      writer.SetBoolean("UseDefaultCache", UseDefaultCache);
      writer.SetBoolean("AutoSend", AutoSend);
      writer.SetString("CurrentComponentState", CurrentComponentState);
      writer.SetString("BaseId", BaseId);
      writer.SetString("KitName", Kit?.Name);

      var owSer = string.Join("\n", OutputWrappers.Select(ow => $"{ow.ServerUrl}\t{ow.StreamId}\t{ow.CommitId}"));
      writer.SetString("OutputWrappers", owSer);

      return base.Write(writer);
    }

    public override bool Read(GH_IReader reader)
    {
      UseDefaultCache = reader.GetBoolean("UseDefaultCache");
      AutoSend = reader.GetBoolean("AutoSend");
      CurrentComponentState = reader.GetString("CurrentComponentState");
      BaseId = reader.GetString("BaseId");

      var wrappersRaw = reader.GetString("OutputWrappers");
      var wrapperLines = wrappersRaw.Split('\n');
      if (wrapperLines != null && wrapperLines.Length != 0 && wrappersRaw != "")
      {
        foreach (var line in wrapperLines)
        {
          var pieces = line.Split('\t');
          OutputWrappers.Add(new StreamWrapper { ServerUrl = pieces[0], StreamId = pieces[1], CommitId = pieces[2] });
        }

        if (OutputWrappers.Count != 0)
        {
          JustPastedIn = true;
        }
      }

      return base.Read(reader);
    }

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
      pManager.AddGenericParameter("Data", "D", "The data to send.",
        GH_ParamAccess.tree);
      pManager.AddGenericParameter("Stream", "S", "Stream(s) and/or transports to send to.", GH_ParamAccess.tree);
      pManager.AddTextParameter("Message", "M", "Commit message. If left blank, one will be generated for you.",
        GH_ParamAccess.tree, "");

      Params.Input[2].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
      pManager.AddGenericParameter("Stream", "S",
        "Stream or streams pointing to the created commit", GH_ParamAccess.list);
    }

    public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
    {
      Menu_AppendSeparator(menu);

      var cacheMi = Menu_AppendItem(menu, "Use default cache", (s, e) => UseDefaultCache = !UseDefaultCache, true,
        UseDefaultCache);
      cacheMi.ToolTipText =
        "It's advised you always use the default cache, unless you are providing a list of custom transports and you understand the consequences.";

      var autoSendMi = Menu_AppendItem(menu, "Send automatically", (s, e) =>
      {
        AutoSend = !AutoSend;
        Rhino.RhinoApp.InvokeOnUiThread((Action)delegate { OnDisplayExpired(true); });
      }, true, AutoSend);
      autoSendMi.ToolTipText =
        "Toggle automatic data sending. If set, any change in any of the input parameters of this component will start sending.\n Please be aware that if a new send starts before an old one is finished, the previous operation is cancelled.";

      if (OutputWrappers.Count != 0)
      {
        Menu_AppendSeparator(menu);
        foreach (var ow in OutputWrappers)
        {
          Menu_AppendItem(menu, $"View commit {ow.CommitId} @ {ow.ServerUrl} online ↗",
            (s, e) => System.Diagnostics.Process.Start($"{ow.ServerUrl}/streams/{ow.StreamId}/commits/{ow.CommitId}"));
        }
      }
      Menu_AppendSeparator(menu);

      if (CurrentComponentState == "sending")
      {
        Menu_AppendItem(menu, "Cancel Send", (s, e) =>
        {
          CurrentComponentState = "expired";
          RequestCancellation();
        });
      }

      base.AppendAdditionalMenuItems(menu);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {

      // Set output data in a "first run" event. Note: we are not persisting the actual "sent" object as it can be very big.
      if (JustPastedIn)
      {
        base.SolveInstance(DA);
        return;
      }

      if ((AutoSend || CurrentComponentState == "primed_to_send" || CurrentComponentState == "sending") &&
        !JustPastedIn)
      {
        CurrentComponentState = "sending";

        // Delegate control to parent async component.
        base.SolveInstance(DA);
        return;
      }
      else if (!JustPastedIn)
      {
        DA.SetDataList(0, OutputWrappers);
        //DA.SetData(1, BaseId);
        CurrentComponentState = "expired";
        Message = "Expired";
        OnDisplayExpired(true);
      }
    }

    public override void DisplayProgress(object sender, ElapsedEventArgs e)
    {
      if (Workers.Count == 0)
      {
        return;
      }

      Message = "";
      var total = 0.0;
      foreach (var kvp in ProgressReports)
      {
        Message += $"{kvp.Key}: {kvp.Value}\n";
        total += kvp.Value;
      }

      OverallProgress = total / ProgressReports.Keys.Count();

      Rhino.RhinoApp.InvokeOnUiThread((Action)delegate { OnDisplayExpired(true); });
    }

    public override void DocumentContextChanged(GH_Document document, GH_DocumentContext context)
    {
      switch (context)
      {
        case GH_DocumentContext.Loaded:
          OnDisplayExpired(true);
          break;

        case GH_DocumentContext.Unloaded:
          // Will execute every time a document becomes inactive (in background or closing file.)
          //Correctly dispose of the client when changing documents to prevent subscription handlers being called in background.
          RequestCancellation();
          break;
      }

      base.DocumentContextChanged(document, context);
    }

  }

  public class SendComponentWorker : WorkerInstance
  {
    GH_Structure<IGH_Goo> DataInput;
    GH_Structure<IGH_Goo> _TransportsInput;
    GH_Structure<GH_String> _MessageInput;

    string InputState;

    List<ITransport> Transports;

    Base ObjectToSend;
    long TotalObjectCount;

    Action<ConcurrentDictionary<string, int>> InternalProgressAction;

    Action<string, Exception> ErrorAction;

    List<(GH_RuntimeMessageLevel, string)> RuntimeMessages { get; set; } = new List<(GH_RuntimeMessageLevel, string)>();

    List<StreamWrapper> OutputWrappers = new List<StreamWrapper>();

    public string BaseId { get; set; }

    public SendComponentWorker(GH_Component p) : base(p)
    {
      RuntimeMessages = new List<(GH_RuntimeMessageLevel, string)>();
    }

    public override WorkerInstance Duplicate() => new SendComponentWorker(Parent);

    private System.Diagnostics.Stopwatch stopwatch;

    public override void GetData(IGH_DataAccess DA, GH_ComponentParamServer Params)
    {
      DA.GetDataTree(0, out DataInput);
      DA.GetDataTree(1, out _TransportsInput);
      DA.GetDataTree(2, out _MessageInput);

      OutputWrappers = new List<StreamWrapper>();

      stopwatch = new System.Diagnostics.Stopwatch();
      stopwatch.Start();
    }

    public override void DoWork(Action<string, double> ReportProgress, Action Done)
    {
      try
      {
        var sendComponent = (SendComponent)Parent;
        if (sendComponent.JustPastedIn)
        {
          Done();
          return;
        }

        if (CancellationToken.IsCancellationRequested)
        {
          sendComponent.CurrentComponentState = "expired";
          return;
        }

        //the active document may have changed
        sendComponent.Converter.SetContextDocument(RhinoDoc.ActiveDoc);

        // Note: this method actually converts the objects to speckle too
        try
        {
          int convertedCount = 0;
          var converted = Utilities.DataTreeToNestedLists(DataInput, sendComponent.Converter, CancellationToken, () =>
          {
            ReportProgress("Conversion", Math.Round(convertedCount++ / (double)DataInput.DataCount, 2));
          });

          if (convertedCount == 0)
          {
            RuntimeMessages.Add((GH_RuntimeMessageLevel.Error, "Zero objects converted successfully. Send stopped."));
            Done();
            return;
          }

          ObjectToSend = new Base();
          ObjectToSend["@data"] = converted;
          TotalObjectCount = ObjectToSend.GetTotalChildrenCount();
        }
        catch (Exception e)
        {
          RuntimeMessages.Add((GH_RuntimeMessageLevel.Error, e.Message));
          Done();
          return;
        }

        if (CancellationToken.IsCancellationRequested)
        {
          sendComponent.CurrentComponentState = "expired";
          return;
        }

        // Part 2: create transports

        Transports = new List<ITransport>();

        if (_TransportsInput.DataCount == 0)
        {
          // TODO: Set default account + "default" user stream
        }

        var transportBranches = new Dictionary<ITransport, string>();
        int t = 0;
        foreach (var data in _TransportsInput)
        {
          var transport = data.GetType().GetProperty("Value").GetValue(data);

          if (transport is string s)
          {
            try
            {
              transport = new StreamWrapper(s);
            }
            catch (Exception e)
            {
              // TODO: Check this with team.
              RuntimeMessages.Add((GH_RuntimeMessageLevel.Warning, e.Message));
            }
          }

          if (transport is StreamWrapper sw)
          {
            if (sw.Type == StreamWrapperType.Undefined)
            {
              RuntimeMessages.Add((GH_RuntimeMessageLevel.Warning, "Input stream is invalid."));
              continue;
            }

            if (sw.Type == StreamWrapperType.Commit)
            {
              RuntimeMessages.Add((GH_RuntimeMessageLevel.Warning, "Cannot push to a specific commit stream url."));
              continue;
            }

            if (sw.Type == StreamWrapperType.Object)
            {
              RuntimeMessages.Add((GH_RuntimeMessageLevel.Warning, "Cannot push to a specific object stream url."));
              continue;
            }

            Account acc;
            try
            {
              acc = sw.GetAccount().Result;
            }
            catch (Exception e)
            {
              RuntimeMessages.Add((GH_RuntimeMessageLevel.Warning, e.InnerException?.Message ?? e.Message));
              continue;
            }
            
            sendComponent.Tracker.TrackNodeSend(acc, sendComponent.AutoSend);

            var serverTransport = new ServerTransport(acc, sw.StreamId) { TransportName = $"T{t}" };
            transportBranches.Add(serverTransport, sw.BranchName ?? "main");
            Transports.Add(serverTransport);
          }
          else if (transport is ITransport otherTransport)
          {
            otherTransport.TransportName = $"T{t}";
            Transports.Add(otherTransport);
          }

          t++;
        }

        if (Transports.Count == 0)
        {
          RuntimeMessages.Add((GH_RuntimeMessageLevel.Warning, "Could not identify any valid transports to send to."));
          Done();
          return;
        }

        InternalProgressAction = (dict) =>
        {
          foreach (var kvp in dict)
          {
            //NOTE: progress set to indeterminate until the TotalChildrenCount is correct
            //ReportProgress(kvp.Key, (double)kvp.Value / TotalObjectCount);
            ReportProgress(kvp.Key, (double)kvp.Value);
          }
        };

        ErrorAction = (transportName, exception) =>
        {
          // TODO: This message condition should be removed once the `link sharing` issue is resolved server-side.
          var msg = exception.Message.Contains("401")
            ? $"You don't have access to this transport , or it doesn't exist."
            : exception.Message;
          RuntimeMessages.Add((GH_RuntimeMessageLevel.Error, $"{transportName}: {msg}"));
          Done();
          var asyncParent = (GH_AsyncComponent)Parent;
          asyncParent.CancellationSources.ForEach(source =>
          {
            if (source.Token != CancellationToken)
              source.Cancel();
          });
        };

        if (CancellationToken.IsCancellationRequested)
        {
          sendComponent.CurrentComponentState = "expired";
          return;
        }

        // Part 3: actually send stuff!

        var task = Task.Run(async () =>
        {
          if (CancellationToken.IsCancellationRequested)
          {
            sendComponent.CurrentComponentState = "expired";
            return;
          }

          // Part 3.1: persist the objects
          BaseId = await Operations.Send(
            ObjectToSend,
            CancellationToken,
            Transports,
            useDefaultCache: sendComponent.UseDefaultCache,
            onProgressAction: InternalProgressAction,
            onErrorAction: ErrorAction, disposeTransports: true);

          // 3.2 Create commits for any server transport present

          var message = _MessageInput.get_FirstItem(true).Value;
          if (message == "")
          {
            message = $"Pushed {TotalObjectCount} elements from Grasshopper.";
          }

          var prevCommits = sendComponent.OutputWrappers;

          foreach (var transport in Transports)
          {
            if (CancellationToken.IsCancellationRequested)
            {
              sendComponent.CurrentComponentState = "expired";
              return;
            }

            if (!(transport is ServerTransport))
            {
              continue; // skip non-server transports (for now)
            }

            try
            {
              var client = new Client(((ServerTransport)transport).Account);
              var branch = transportBranches.ContainsKey(transport) ? transportBranches[transport] : "main";

              var commitCreateInput = new CommitCreateInput
              {
                branchName = branch,
                message = message,
                objectId = BaseId,
                streamId = ((ServerTransport)transport).StreamId,
                sourceApplication = Extras.Utilities.GetVersionedAppName()
              };

              // Check to see if we have a previous commit; if so set it.
              var prevCommit = prevCommits.FirstOrDefault(c =>
                c.ServerUrl == client.ServerUrl && c.StreamId == ((ServerTransport)transport).StreamId);
              if (prevCommit != null)
              {
                commitCreateInput.parents = new List<string>() { prevCommit.CommitId };
              }

              var commitId = await client.CommitCreate(CancellationToken, commitCreateInput);

              var wrapper = new StreamWrapper($"{client.Account.serverInfo.url}/streams/{((ServerTransport)transport).StreamId}/commits/{commitId}?u={client.Account.userInfo.id}");
              OutputWrappers.Add(wrapper);
            }
            catch (Exception e)
            {
              ErrorAction.Invoke("Commits", e);
            }
          }

          if (CancellationToken.IsCancellationRequested)
          {
            sendComponent.CurrentComponentState = "expired";
            Done();
          }

          Done();
        }, CancellationToken);
      }
      catch (Exception e)
      {

        // If we reach this, something happened that we weren't expecting...
        Logging.Log.CaptureException(e);
        RuntimeMessages.Add((GH_RuntimeMessageLevel.Error, "Something went terribly wrong... " + e.Message));
        //Parent.Message = "Error";
        //((SendComponent)Parent).CurrentComponentState = "expired";
        Done();
      }
    }

    public override void SetData(IGH_DataAccess DA)
    {
      stopwatch.Stop();

      if (((SendComponent)Parent).JustPastedIn)
      {
        ((SendComponent)Parent).JustPastedIn = false;
        DA.SetDataList(0, ((SendComponent)Parent).OutputWrappers);
        return;
      }

      if (CancellationToken.IsCancellationRequested)
      {
        ((SendComponent)Parent).CurrentComponentState = "expired";
        return;
      }

      foreach (var (level, message) in RuntimeMessages)
      {
        Parent.AddRuntimeMessage(level, message);
      }

      DA.SetDataList(0, OutputWrappers);

      ((SendComponent)Parent).CurrentComponentState = "up_to_date";
      ((SendComponent)Parent).OutputWrappers = OutputWrappers; // ref the outputs in the parent too, so we can serialise them on write/read

      ((SendComponent)Parent).BaseId = BaseId; // ref the outputs in the parent too, so we can serialise them on write/read

      ((SendComponent)Parent).OverallProgress = 0;

      var hasWarnings = RuntimeMessages.Count > 0;
      if (!hasWarnings)
      {
        Parent.AddRuntimeMessage(GH_RuntimeMessageLevel.Remark,
          $"Successfully pushed {TotalObjectCount} objects to {(((SendComponent)Parent).UseDefaultCache ? Transports.Count - 1 : Transports.Count)} transports.");
        Parent.AddRuntimeMessage(GH_RuntimeMessageLevel.Remark,
          $"Send duration: {stopwatch.ElapsedMilliseconds / 1000f}s");
        foreach (var t in Transports)
        {
          if (!(t is ServerTransport st))
          {
            continue;
          }

          var mb = st.TotalSentBytes / 1e6;
          Parent.AddRuntimeMessage(GH_RuntimeMessageLevel.Remark,
            $"{t.TransportName} avg {(mb / (stopwatch.ElapsedMilliseconds / 1000f)):0.00} MB/s");
        }
      }
    }
  }

  public class SendComponentAttributes : GH_ComponentAttributes
  {
    private bool _selected;
    Rectangle ButtonBounds { get; set; }

    public SendComponentAttributes(GH_Component owner) : base(owner) { }

    public override bool Selected
    {
      get
      {
        return _selected;
      }
      set
      {
        Owner.Params.ToList().ForEach(p => p.Attributes.Selected = value);
        _selected = value;
      }
    }

    protected override void Layout()
    {
      base.Layout();

      var baseRec = GH_Convert.ToRectangle(Bounds);
      baseRec.Height += 26;

      var btnRec = baseRec;
      btnRec.Y = btnRec.Bottom - 26;
      btnRec.Height = 26;
      btnRec.Inflate(-2, -2);

      Bounds = baseRec;
      ButtonBounds = btnRec;
    }

    protected override void Render(GH_Canvas canvas, Graphics graphics, GH_CanvasChannel channel)
    {
      base.Render(canvas, graphics, channel);

      var state = ((SendComponent)Owner).CurrentComponentState;

      if (channel == GH_CanvasChannel.Objects)
      {
        if (((SendComponent)Owner).AutoSend)
        {
          var autoSendButton =
            GH_Capsule.CreateTextCapsule(ButtonBounds, ButtonBounds, GH_Palette.Blue, "Auto Send", 2, 0);

          autoSendButton.Render(graphics, Selected, Owner.Locked, false);
          autoSendButton.Dispose();
        }
        else
        {
          var palette = (state == "expired" || state == "up_to_date") ? GH_Palette.Black : GH_Palette.Transparent;
          //NOTE: progress set to indeterminate until the TotalChildrenCount is correct
          //var text = state == "sending" ? $"{((SendComponent)Owner).OverallProgress}" : "Send";
          var text = state == "sending" ? $"Sending..." : "Send";

          var button = GH_Capsule.CreateTextCapsule(ButtonBounds, ButtonBounds, palette, text, 2,
            state == "expired" ? 10 : 0);
          button.Render(graphics, Selected, Owner.Locked, false);
          button.Dispose();
        }
      }
    }

    public override GH_ObjectResponse RespondToMouseDown(GH_Canvas sender, GH_CanvasMouseEvent e)
    {
      if (e.Button == MouseButtons.Left)
      {
        if (((RectangleF)ButtonBounds).Contains(e.CanvasLocation))
        {
          if (((SendComponent)Owner).AutoSend)
          {
            ((SendComponent)Owner).AutoSend = false;
            Owner.OnDisplayExpired(true);
            return GH_ObjectResponse.Handled;
          }
          if (((SendComponent)Owner).CurrentComponentState == "sending")
          {
            return GH_ObjectResponse.Handled;
          }

          ((SendComponent)Owner).CurrentComponentState = "primed_to_send";
          Owner.ExpireSolution(true);
          return GH_ObjectResponse.Handled;
        }
      }

      return base.RespondToMouseDown(sender, e);
    }

  }
}
